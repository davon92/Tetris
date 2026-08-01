/// <summary>
/// One full-screen mode: the title menu, the roster picker, a story scene, or
/// a battle. Adding a mode means adding an implementation and routing to it —
/// no existing screen has to change.
/// </summary>
public interface IGameScreen
{
    void Enter();

    void Exit();

    void Tick(float deltaTime, in UiInput input);

    /// <summary>Called from <c>OnGUI</c> inside the reference canvas.</summary>
    void Draw();
}
