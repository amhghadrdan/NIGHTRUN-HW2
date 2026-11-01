using UnityEngine;
namespace NightRun
{
    /// <summary>All traffic and pickups share a resettable pooled lifecycle.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public int Lane { get; private set; }
        public bool Consumed { get; private set; }
        public virtual float ForwardSpeed => 0;
        public abstract bool IsObstacle { get; }
        public virtual float HalfLength => IsObstacle ? 2.3f : .7f;
        public virtual void OnSpawn(int lane, float z, float laneWidth)
        {
            Lane = lane; Consumed = false;
            transform.position = new Vector3((lane - 1) * laneWidth, 0, z);
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(true);
        }
        public virtual void Tick(float dt, float playerSpeed)
        {
            transform.position += Vector3.forward * ((ForwardSpeed - playerSpeed) * dt);
            if (!IsObstacle) transform.GetChild(0).Rotate(0, 80 * dt, 0, Space.Self);
        }
        public bool TryInteract(GameManager game)
        {
            if (Consumed) return false;
            // Even a hit during invulnerability consumes this obstacle's contact once.
            Consumed = true;
            OnInteract(game);
            return true;
        }
        protected abstract void OnInteract(GameManager game);
        public virtual void OnDespawn() { gameObject.SetActive(false); }
    }
}
