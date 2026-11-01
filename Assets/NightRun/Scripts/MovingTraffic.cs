namespace NightRun
{
    public sealed class MovingTraffic : Interactable
    {
        public override bool IsObstacle => true;
        public override float ForwardSpeed => 22;
        protected override void OnInteract(GameManager game) { game.Damage(transform.position); }
    }
}
