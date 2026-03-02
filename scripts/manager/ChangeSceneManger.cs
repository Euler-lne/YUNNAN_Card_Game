using Godot;

public partial class ChangeSceneManger : Node
{
    public static ChangeSceneManger Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance == null)
            Instance = this;
        else
            QueueFree();
    }

    public void ChangeScene(string sceneName)
    {
        GetTree().ChangeSceneToFile(sceneName);
    }


}