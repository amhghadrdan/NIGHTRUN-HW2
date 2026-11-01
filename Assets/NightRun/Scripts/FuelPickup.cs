namespace NightRun
{
    public sealed class FuelPickup : Interactable
    {
        public override bool IsObstacle => false;
        protected override void OnInteract(GameManager game) { game.Collect(true, transform.position); }
    }
}
