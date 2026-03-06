using Godot;
using System;
using Euler.Event;

public partial class HoleUI : Control
{
	private Button bigButton;
	private Button smallButton;
	private HBoxContainer hBoxContainer;
	private Info info;

	public override void _Ready()
	{
		info = GetNode<Info>("Info");
		bigButton = GetNode<Button>("HBoxContainer/BigButton");
		smallButton = GetNode<Button>("HBoxContainer/SmallButton");

		hBoxContainer = GetNode<HBoxContainer>("HBoxContainer");

		bigButton.Pressed += OnBigButtonPressed;
		smallButton.Pressed += OnSmallButtonPressed;

		DealEvent.ChooseHoleEvent += SetVisiable;
		DealEvent.ServerNotifyChooseHoleResultEvent += SetInfo;

		SetInVisiable();

	}

	public override void _ExitTree()
	{
		bigButton.Pressed -= OnBigButtonPressed;
		smallButton.Pressed -= OnSmallButtonPressed;
		DealEvent.ChooseHoleEvent -= SetVisiable;

		DealEvent.ServerNotifyChooseHoleResultEvent -= SetInfo;

	}

	private void SetInfo(bool isBig)
	{
		if (isBig)
			info.SetInfo("庄家选择遇大");
		else
			info.SetInfo("庄家选择遇小");

	}

	private void SetVisiable()
	{
		hBoxContainer.Visible = true;
	}
	private void SetInVisiable()
	{
		hBoxContainer.Visible = false;
	}


	private void OnBigButtonPressed()
	{
		SetInVisiable();
		RpcId(1, nameof(RpcClientChooseHoleResult), true);
	}
	private void OnSmallButtonPressed()
	{
		SetInVisiable();
		RpcId(1, nameof(RpcClientChooseHoleResult), false);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcClientChooseHoleResult(bool isBig)
	{
		// 服务器收到消息，通知给DealManager
		DealEvent.OnClientNotifyChooseHoleResultEvent(isBig);
	}

}
