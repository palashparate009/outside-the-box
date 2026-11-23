namespace OutsideTheBox
{
    // Keep this script and its GUID so existing scene and prefab references remain valid.
    public sealed class BoardView : BoardViewImplementation
    {
        protected override void Awake() => base.Awake();
        protected override void FixedUpdate() => base.FixedUpdate();
        protected override void Update() => base.Update();
    }
}
