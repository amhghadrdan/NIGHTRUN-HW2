using System.Collections.Generic;
using NightRun;
using NUnit.Framework;
using UnityEngine;

public class RunTests
{
    RunConfig config;
    [SetUp] public void Setup() { config = ScriptableObject.CreateInstance<RunConfig>(); }
    [TearDown] public void Teardown() { Object.DestroyImmediate(config); }
    [Test] public void FuelExhaustionEndsRun()
    {
        var run = new RunModel(config); run.Tick(50.1f);
        Assert.That(run.Reason, Is.EqualTo(EndReason.OutOfFuel)); Assert.That(run.Fuel, Is.Zero);
    }
    [Test] public void ContactCannotConsumeThreeLivesAtOnce()
    {
        var run = new RunModel(config); Assert.IsTrue(run.Hit()); Assert.IsFalse(run.Hit());
        Assert.That(run.Lives, Is.EqualTo(2)); run.Tick(1.21f); run.Hit(); run.Tick(1.21f); run.Hit();
        Assert.That(run.Reason, Is.EqualTo(EndReason.Wrecked));
    }
    [Test] public void NitroRequiresReleaseAfterExhaustion()
    {
        var run = new RunModel(config); run.SetBoost(true); run.Tick(5);
        Assert.IsFalse(run.Boosting); run.Tick(5); Assert.IsFalse(run.Boosting);
        run.SetBoost(false); run.SetBoost(true); run.Tick(.1f); Assert.IsTrue(run.Boosting);
    }
    [Test] public void NitroChargesOnlyAfterDelay()
    {
        var run = new RunModel(config); run.SetBoost(true); run.Tick(1); float level = run.Nitro;
        run.SetBoost(false); run.Tick(1.4f); Assert.That(run.Nitro, Is.EqualTo(level).Within(.02));
        run.Tick(.6f); Assert.That(run.Nitro, Is.InRange(level + 4.8f, level + 5.2f));
    }
    [Test] public void BoostCostsMoreFuelAndCoversMoreDistance()
    {
        var normal = new RunModel(config); var boost = new RunModel(config); boost.SetBoost(true);
        normal.Tick(2); boost.Tick(2);
        Assert.That(100 - boost.Fuel, Is.EqualTo((100 - normal.Fuel) * 1.5f).Within(.02));
        Assert.That(boost.Distance, Is.EqualTo(normal.Distance * 1.25f).Within(.03));
    }
    [Test] public void FuelClampsAndCoinScoreAddsToDistance()
    {
        var run = new RunModel(config); run.CollectFuel(); Assert.That(run.Fuel, Is.EqualTo(100));
        run.Tick(10); run.CollectFuel(); run.CollectCoin(); Assert.That(run.Fuel, Is.EqualTo(100));
        Assert.That(run.Score, Is.EqualTo(Mathf.FloorToInt(run.Distance) + 100));
    }
    [Test] public void RestartClearsAllRunState()
    {
        var run = new RunModel(config); run.Tick(5); run.Hit(); run.CollectCoin(); run.SetBoost(true); run.Reset();
        Assert.That(run.Score, Is.Zero); Assert.That(run.Lives, Is.EqualTo(3)); Assert.That(run.Fuel, Is.EqualTo(100));
        Assert.That(run.Nitro, Is.EqualTo(100)); Assert.IsFalse(run.Invulnerable); Assert.IsFalse(run.Boosting);
    }
    [Test] public void RulesAreIndependentOfFrameRate()
    {
        var slow = new RunModel(config); var fast = new RunModel(config); slow.SetBoost(true); fast.SetBoost(true);
        for (int i = 0; i < 90; i++) slow.Tick(1f / 30);
        for (int i = 0; i < 432; i++) fast.Tick(1f / 144);
        Assert.That(slow.Distance, Is.EqualTo(fast.Distance).Within(.08));
        Assert.That(slow.Nitro, Is.EqualTo(fast.Nitro).Within(.04));
    }
    [Test] public void SweptCollisionCatchesLargeFrameStep()
    {
        Assert.IsTrue(RoadWorld.SweptContact(15, -15, 0, 0, 0, 2.3f, 1.6f));
        Assert.IsFalse(RoadWorld.SweptContact(15, -15, 0, 0, 3.6f, 2.3f, 1.6f));
        Assert.IsFalse(RoadWorld.SweptContact(40, 30, 0, 3.6f, 0, 2.3f, 1.6f));
    }
    [Test] public void PlannerRejectsAnUnavoidableWall()
    {
        var wall = new List<TrafficForecast> { new TrafficForecast(0, 20, 0), new TrafficForecast(1, 20, 0), new TrafficForecast(2, 20, 0) };
        Assert.IsFalse(RoadPlanner.IsSafe(wall, 1, 240 / 3.6f));
    }
    [Test] public void PlannerAllowsAContinuousOpenLaneAtAllSpeeds()
    {
        var traffic = new List<TrafficForecast>();
        for (int i = 0; i < 15; i++) { traffic.Add(new TrafficForecast(0, 140 + i * 35, 0)); traffic.Add(new TrafficForecast(2, 140 + i * 35, i % 2 == 0 ? 22 : 0)); }
        for (int speed = 140; speed <= 240; speed += 10) Assert.IsTrue(RoadPlanner.IsSafe(traffic, 1, speed / 3.6f));
    }
    [Test] public void PlannerLooksFarEnoughAheadForSlowClosingMovingTraffic()
    {
        var wall = new List<TrafficForecast> { new TrafficForecast(0, 220, 22), new TrafficForecast(1, 220, 22), new TrafficForecast(2, 220, 22) };
        Assert.IsFalse(RoadPlanner.IsSafe(wall, 1, 140 / 3.6f));
    }
    [Test] public void CityBuildingsNeverOverlapIncludingSegmentSeams()
    {
        for (int seed = 0; seed < 30; seed++) for (int segment = 0; segment < 40; segment++)
        {
            var plots = EnvironmentLayout.Create(segment, seed);
            var next = EnvironmentLayout.Create(segment + 1, seed);
            for (int a = 0; a < plots.Count; a++)
            {
                for (int b = a + 1; b < plots.Count; b++) Assert.IsFalse(EnvironmentLayout.Overlaps(plots[a], plots[b]), "Same-block overlap");
                foreach (var neighbor in next) Assert.IsFalse(EnvironmentLayout.Overlaps(plots[a], neighbor, EnvironmentLayout.SegmentLength), "Recycling seam overlap");
            }
        }
    }
    [Test] public void CityReservesTheHighwayAndKeepsRoofsBelowSkyView()
    {
        var kinds = new HashSet<BuildingKind>();
        for (int segment = 0; segment < 40; segment++) foreach (var plot in EnvironmentLayout.Create(segment))
        {
            Assert.That(Mathf.Abs(plot.Center.x) - plot.Size.x / 2, Is.GreaterThanOrEqualTo(22));
            Assert.That(plot.Size.y, Is.LessThanOrEqualTo(14.4f));
            Assert.That(plot.Footprint.min.z, Is.GreaterThan(1));
            Assert.That(plot.Footprint.max.z, Is.LessThan(59));
            kinds.Add(plot.Kind);
        }
        Assert.That(kinds.Count, Is.EqualTo(7));
    }
}
