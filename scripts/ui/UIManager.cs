using Godot;
using System;

public partial class UIManager : Control
{
	private Button startButton;
	private Label currentPlayerNumber;
	private DeclareContainer declareContainer;

	public override void _Ready()
	{
		startButton = GetNode<Button>("VBoxContainer/StartButton");
		currentPlayerNumber = GetNode<Label>("VBoxContainer/CurrentPlayerNumber");
		declareContainer = GetNode<DeclareContainer>("DeclareContainer");

		declareContainer.Visible = false;
		// 客户端点击叫主，通知服务器
		declareContainer.OnDeclarePressed += ClientRequestManager.Instance.SendDeclareRequest;
		// 客户端点击确认，通知服务器

		declareContainer.OnConfirmPressed += ClientRequestManager.Instance.SendConfirmDeclare;

		declareContainer.OnCancelButtonPressed += ClientRequestManager.Instance.SendCancelDarkDeclare;

		if (Multiplayer.IsServer())
		{
			startButton.Visible = true;
			currentPlayerNumber.Visible = true;
		}
		else
		{
			startButton.Visible = false;
			currentPlayerNumber.Visible = false;
		}
	}
	#region 叫主UI显示相关
	public void Declare(DeclareOption option)
	{
		declareContainer.Visible = option != DeclareOption.NONE;
		if (option == DeclareOption.NONE) return;
		declareContainer.Declare(option);
	}

	public void DeclareButtonPressed(bool isValid)
	{
		declareContainer.Visible = true;
		declareContainer.IsDeclare = false;
	}
	#endregion

	#region 服务器开始游戏相关
	public void UpdatePlayerCount(int count)
	{
		currentPlayerNumber.Text = "当前人数: " + count;
	}

	public void ConnectStartButtonPressed(Action actions)
	{
		startButton.Pressed += () =>
		{
			startButton.Visible = false;
			currentPlayerNumber.Visible = false;
			actions?.Invoke();
		};
	}
	#endregion
}