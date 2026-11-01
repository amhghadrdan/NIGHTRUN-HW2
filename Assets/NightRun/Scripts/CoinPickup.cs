namespace NightRun
{
    public sealed class CoinPickup : Interactable
    {
        public override bool IsObstacle => false;
        protected override void OnInteract(GameManager game) { game.Collect(false, transform.position); }
    }
}
