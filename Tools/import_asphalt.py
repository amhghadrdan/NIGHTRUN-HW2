#!/usr/bin/env python3
"""Inventory a user-supplied Asphalt Nitro data01.zip and convert static visuals.

Requires numpy, Pillow, texture2ddecoder and lz4. Reads PIG v100 geometry and
PVR3 textures; never executes game code or decrypts protected content. The
binary layout was investigated using RaduMC/PIG2FBX as a format reference.
Outputs are local research assets, not a grant of redistribution rights.
"""
import argparse
import collections
import hashlib
import io
import json
import pathlib
import re
import struct
import zipfile

import numpy as np
from PIL import Image


class Reader:
    def __init__(self, data):
        self.data = data
        self.pos = 0

    def take(self, size):
        if size < 0 or self.pos + size > len(self.data):
            raise ValueError(f"Truncated record at {self.pos}, size {size}")
        out = self.data[self.pos:self.pos + size]
        self.pos += size
        return out

    def read(self, fmt):
        values = struct.unpack('<' + fmt, self.take(struct.calcsize('<' + fmt)))
        return values[0] if len(values) == 1 else values

    def text(self):
        return self.take(self.read('H')).decode('utf-8')

    def marker(self):
        if self.read('I') != 100:
            raise ValueError(f"Unsupported PIG record at {self.pos - 4}")


def transform(translation, quaternion, scale):
    x, y, z, w = quaternion
    rotation = np.array([
        [1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
        [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
        [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]])
    result = np.eye(4)
    result[:3, :3] = rotation @ np.diag(scale)
    result[:3, 3] = translation
    return result


def packed_block(reader):
    size = reader.read('I')
    if size:
        return reader.take(size)
    import lz4.block
    compressed, expanded = reader.read('II')
    return lz4.block.decompress(reader.take(compressed), uncompressed_size=expanded)


def geometry(data, count, flags, indices):
    reader = Reader(data)
    arrays = {}
    for bit in range(8):
        if not flags & (1 << bit):
            continue
        reader.take(reader.read('H'))
        start = reader.pos
        if bit in (0, 7, 8):
            components = 4 if bit == 0 else 2
            packed_size = count * components * 2
            # Each attribute begins on the next 16-byte boundary, with a u16
            # padding length immediately preceding it. Quantized streams are
            # identified by the following attribute's own alignment record.
            expected = 16 - (packed_size % 16) - 2
            next_pos = start + packed_size
            quantized = next_pos + 2 <= len(data) and struct.unpack_from('<h', data, next_pos)[0] == expected
            if quantized:
                a = np.frombuffer(reader.take(packed_size), dtype='<i2').reshape(count, components).astype(np.float32) / 32767
                if bit == 0:
                    a = a[:, :3]
            else:
                components = 3 if bit == 0 else 2
                a = np.frombuffer(reader.take(count * components * 4), dtype='<f4').reshape(count, components).copy()
            arrays[bit] = a
        elif bit == 1:
            a = np.frombuffer(reader.take(count * 4), dtype='i1').reshape(count, 4)
            arrays[bit] = a[:, :3].astype(np.float32) / 127
        elif bit == 10:
            reader.take(count * 8)
        else:
            reader.take(count * 4)
    faces = np.frombuffer(data[-indices * 2:], dtype='<u2').copy()
    if indices % 3 or (indices and faces.max() >= count):
        raise ValueError('Invalid triangle index stream')
    return arrays, faces.reshape(-1, 3)


def read_pig(data):
    reader = Reader(data)
    reader.marker()
    nodes = []
    for _ in range(reader.read('H')):
        reader.marker()
        name = reader.text()
        reader.take(1)
        parent = reader.read('h')
        matrix = transform(reader.read('3f'), reader.read('4f'), reader.read('3f'))
        reader.take(6)
        nodes.append((name, matrix, parent))
    world = {}
    def resolve(index, visiting=()):
        if index in world:
            return world[index]
        if index in visiting or index < 0 or index >= len(nodes):
            raise ValueError('Invalid node hierarchy')
        name, matrix, parent = nodes[index]
        world[index] = resolve(parent, visiting + (index,)) @ matrix if parent >= 0 else matrix
        return world[index]
    for index in range(len(nodes)):
        resolve(index)
    reader.take(1)
    meshes = []
    for _ in range(reader.read('H')):
        reader.marker()
        node_id, lods = reader.read('IH')
        for lod_index in range(lods):
            lod = reader.read('B')
            reader.marker()
            extra = reader.read('H')
            reader.take(24)
            for _ in range(reader.read('H')):
                reader.marker()
                flags, fvf = reader.read('II')
                reader.take(12)
                offset = reader.read('3f') if flags & 1 else (0, 0, 0)
                scale = reader.read('3f') if flags & 1 else (1, 1, 1)
                vertex_count, index_count = reader.read('HI')
                material = reader.text()
                textures = [reader.text() for _ in range(reader.read('H'))]
                reader.take(1)
                block = packed_block(reader)
                if lod_index == 0:
                    attrs, faces = geometry(block, vertex_count, fvf, index_count)
                    matrix = world[node_id] @ transform(offset, (0, 0, 0, 1), scale)
                    vertices = attrs[0] @ matrix[:3, :3].T + matrix[:3, 3]
                    normals = attrs.get(1, np.zeros_like(vertices)) @ np.linalg.pinv(matrix[:3, :3])
                    normals /= np.maximum(np.linalg.norm(normals, axis=1, keepdims=True), 1e-8)
                    if np.linalg.det(matrix[:3, :3]) < 0:
                        faces = faces[:, ::-1].copy()
                    uv = attrs.get(7, np.zeros((vertex_count, 2), np.float32)).copy()
                    uv[:, 1] = 1 - uv[:, 1]
                    meshes.append(dict(name=nodes[node_id][0], node=node_id, material=material, textures=textures,
                                       vertices=vertices, normals=normals, uv=uv, faces=faces))
                for _ in range(extra):
                    reader.take(4)
                    size = reader.read('I')
                    if size:
                        reader.take(size)
                    else:
                        compressed, _expanded = reader.read('II')
                        reader.take(compressed)
    originals = list(meshes)
    # PIG uses named references for repeated wheels and ancestor symmetry for
    # half-car bodies. Expand these before exporting static geometry.
    for target, (name, _matrix, _parent) in enumerate(nodes):
        reference = re.search(r'__R-(.*?)__', name)
        if not reference:
            continue
        for source in originals:
            if source['name'] != reference.group(1):
                continue
            mirror = np.eye(4)
            for axis, label in enumerate('XYZ'):
                if '__M-' + label + '__' in name:
                    mirror[axis, axis] = -1
            matrix = world[target] @ mirror @ np.linalg.pinv(world[source['node']])
            clone = dict(source, name=name, node=target)
            clone['vertices'] = source['vertices'] @ matrix[:3, :3].T + matrix[:3, 3]
            clone['normals'] = source['normals'] @ np.linalg.pinv(matrix[:3, :3])
            if np.linalg.det(matrix[:3, :3]) < 0:
                clone['faces'] = source['faces'][:, ::-1].copy()
            meshes.append(clone)
    for mesh in list(meshes):
        parent = nodes[mesh['node']][2]
        while parent >= 0:
            name = nodes[parent][0]
            if '__C__' in name:
                for axis, label in enumerate('XYZ'):
                    if '__M-' + label + '__' in name:
                        mirror = np.eye(4)
                        mirror[axis, axis] = -1
                        matrix = world[parent] @ mirror @ np.linalg.pinv(world[parent])
                        clone = dict(mesh, name=mesh['name'] + '_mirrored_' + label)
                        clone['vertices'] = mesh['vertices'] @ matrix[:3, :3].T + matrix[:3, 3]
                        clone['normals'] = mesh['normals'] @ np.linalg.pinv(matrix[:3, :3])
                        clone['faces'] = mesh['faces'][:, ::-1].copy()
                        meshes.append(clone)
            parent = nodes[parent][2]
    for mesh in meshes:
        # Source is Z-up, with the front of cars facing -Y.
        for attr in ('vertices', 'normals'):
            mesh[attr] = mesh[attr][:, [0, 2, 1]] * (1, 1, -1)
    return meshes


def write_mesh(path, meshes):
    """Compact, little-endian static mesh format consumed by LocalMeshImporter."""
    def text(f, value):
        data = value.encode('utf-8')
        f.write(struct.pack('<I', len(data)))
        f.write(data)
    with path.open('wb') as f:
        f.write(b'NRM1' + struct.pack('<I', len(meshes)))
        for mesh in meshes:
            text(f, mesh['name'])
            text(f, mesh['material'])
            text(f, mesh['textures'][0] if mesh['textures'] else '')
            vertices, faces = mesh['vertices'], mesh['faces']
            f.write(struct.pack('<II', len(vertices), faces.size))
            for attribute in ('vertices', 'normals', 'uv'):
                f.write(np.asarray(mesh[attribute], dtype='<f4').tobytes())
            f.write(np.asarray(faces, dtype='<i4').tobytes())


def decode_texture(data):
    if data[:4] != b'PVR\x03':
        return Image.open(io.BytesIO(data)).convert('RGBA')
    import texture2ddecoder
    version, flags, pixel, colorspace, channel, height, width, depth, surfaces, faces, mips, metadata = struct.unpack_from('<IIQ9I', data)
    if depth != 1 or surfaces != 1 or faces != 1:
        raise ValueError('Texture array/cubemap not supported')
    body = data[52 + metadata:]
    if metadata:
        # Gameloft PVR metadata can describe LZ4-compressed mip levels.
        meta = data[52:52 + metadata]
        if meta[:8] == struct.pack('<II', 1413827072, 878332928):
            import lz4.block
            offset, compressed, expanded = struct.unpack_from('<III', meta, 12)
            body = lz4.block.decompress(body[offset:offset + compressed], uncompressed_size=expanded)
    if pixel == 6:
        rgba = texture2ddecoder.decode_etc1(body, width, height)
        return Image.frombytes('RGBA', (width, height), rgba, 'raw', 'BGRA')
    channels = struct.pack('<Q', pixel)[:4].rstrip(b'\0').decode('ascii')
    bits = struct.pack('<Q', pixel)[4:4+len(channels)]
    if any(b != 8 for b in bits):
        raise ValueError(f'Unsupported PVR pixel layout {pixel}')
    raw = np.frombuffer(body[:width * height * len(channels)], dtype='u1').reshape(height, width, len(channels))
    rgba = np.full((height, width, 4), 255, dtype='u1')
    for i, c in enumerate(channels):
        rgba[:, :, 'rgba'.index(c)] = raw[:, :, i]
    return Image.fromarray(rgba)


def convert(source, output):
    output.mkdir(parents=True, exist_ok=True)
    for folder in ('Models', 'Textures', 'Audio'):
        (output / folder).mkdir(exist_ok=True)
    inventory, converted, failures = [], collections.Counter(), []
    with zipfile.ZipFile(source) as archive:
        for entry in archive.infolist():
            if entry.is_dir():
                continue
            data = archive.read(entry)
            name = pathlib.PurePosixPath(entry.filename)
            inventory.append(dict(path=entry.filename, size=len(data), sha256=hashlib.sha256(data).hexdigest()))
            try:
                if name.suffix == '.pig':
                    meshes = read_pig(data)
                    if meshes:
                        write_mesh(output / 'Models' / (name.stem + '.nrm'), meshes)
                        converted['models'] += 1
                elif name.suffix in ('.tga', '.jpg') and name.parts[0] == 'textures':
                    image = decode_texture(data)
                    alpha_name = entry.filename + '__alpha'
                    if alpha_name in archive.namelist():
                        alpha = decode_texture(archive.read(alpha_name)).convert('L')
                        image.putalpha(alpha.resize(image.size))
                    image.save(output / 'Textures' / (name.stem + '.png'))
                    converted['textures'] += 1
                elif name.suffix == '.ogg':
                    (output / 'Audio' / name.name).write_bytes(data)
                    converted['audio'] += 1
            except Exception as error:
                failures.append(dict(path=entry.filename, error=str(error)))
    report = dict(source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(), entries=len(inventory),
                  converted=dict(converted), failures=failures, inventory=inventory)
    (output / 'inventory.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k: report[k] for k in ('entries', 'converted', 'failures')}, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=pathlib.Path, help='data01.zip from the locally supplied APK')
    parser.add_argument('output', type=pathlib.Path, help='Local conversion destination')
    args = parser.parse_args()
    convert(args.source, args.output)
