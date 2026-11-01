namespace NightRun
{
    public sealed class StationaryTraffic : Interactable
    {
        public override bool IsObstacle => true;
        protected override void OnInteract(GameManager game) { game.Damage(transform.position); }
    }
}
