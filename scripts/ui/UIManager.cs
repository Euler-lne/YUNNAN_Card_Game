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
		declareContainer.Declare(option);
	}

	public void DeclareButtonPressed(bool isValid)
	{
		// 服务器得知按下了叫主按钮然后关闭一些UI显示
		if (isValid) // 合法那么显示确定按钮
			declareContainer.DeclareButtonPressed();
		else // 不合法都取消
			declareContainer.SetInVisiable();
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