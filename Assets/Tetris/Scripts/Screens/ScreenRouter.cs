/// <summary>
/// Holds the active <see cref="IGameScreen"/> and guarantees Exit/Enter pair
/// up on every transition.
/// </summary>
public sealed class ScreenRouter
{
    public IGameScreen Current { get; private set; }

    public void GoTo(IGameScreen screen)
    {
        Current?.Exit();
        Current = screen;
        Current?.Enter();
    }

    public void Tick(float deltaTime, in UiInput input)
    {
        Current?.Tick(deltaTime, input);
    }

    public void Draw()
    {
        Current?.Draw();
    }
}
