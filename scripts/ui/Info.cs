using Godot;
using Euler.Global;
using System;

public partial class Info : Panel
{
	private Label label;
	private Timer timer;
	public override void _Ready()
	{
		label = GetNode<Label>("Label");
		timer = new Timer
		{
			WaitTime = GameSettings.INFO_EXIST_TIME,
			OneShot = true,
			Autostart = false
		};
		AddChild(timer);
		timer.Timeout += TimerOut;
	}

	public override void _ExitTree()
	{
		timer.Timeout -= TimerOut;
	}


	private void TimerOut()
	{
		Visible = false;
	}

	public void SetInfo(string content)
	{
		Visible = true;
		label.Text = content;
		timer.Start();
	}
}
