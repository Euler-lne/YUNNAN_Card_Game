using Godot;
using Euler.Event;
using System.Collections.Generic;
using System;

public partial class DeclareContainer : HBoxContainer
{

	private Button declareButton;
	private Button confirmButton;
	private Button darkDeclareButton;
	private Button cancelButton;

	private HBoxContainer darkDeclareContainer;

	private bool isDeclare = true;
	private DeclareOption currentOption = DeclareOption.NONE;

	private int[] selectCards = [];

	public bool IsDeclare
	{
		get { return isDeclare; }
		set
		{
			isDeclare = value;
			declareButton.Visible = isDeclare;
			confirmButton.Visible = !isDeclare;
		}
	}

	public override void _Ready()
	{
		declareButton = GetNode<Button>("DeclareButton");
		confirmButton = GetNode<Button>("ConfirmButton");
		darkDeclareContainer = GetNode<HBoxContainer>("../DarkDeclareContainer");
		darkDeclareButton = GetNode<Button>("../DarkDeclareContainer/DarkDeclareButton");
		cancelButton = GetNode<Button>("../DarkDeclareContainer/CancelButton");
		isDeclare = true;
		darkDeclareContainer.Visible = false;

		declareButton.Pressed += OnDeclareButtonPressed;
		darkDeclareButton.Pressed += OnConfirmButton;  // 暗主点击了
		confirmButton.Pressed += OnConfirmButton;
		cancelButton.Pressed += OnCancelButton;

		DealEvent.SetDeclareEvent += OnSetDeclareEvent;
		DealEvent.JudgeConfirmEvent += OnJudgeConfirmEvent;
		DealEvent.JudgeDeclareEvent += OnJudgeDeclareEvent;

		EventBus.SelectCardEvent += OnSelectCardEvent;
	}
	public override void _ExitTree()
	{
		DealEvent.SetDeclareEvent -= OnSetDeclareEvent;
		DealEvent.JudgeConfirmEvent -= OnJudgeConfirmEvent;
		DealEvent.JudgeDeclareEvent -= OnJudgeDeclareEvent;

		EventBus.SelectCardEvent -= OnSelectCardEvent;

	}

	private void OnCancelButton()
	{
		RpcId(1, nameof(RpcSendCancelDarkDeclare));
	}

	private void OnConfirmButton()
	{
		long myPeerId = Multiplayer.GetUniqueId();
		RpcId(1, nameof(ServerConfirmDeclare), myPeerId, (int)currentOption, selectCards);
	}


	private void OnDeclareButtonPressed()
	{

		// 触发事件给 ClientRequestManager
		long myPeerId = Multiplayer.GetUniqueId();
		// 调用服务器 RPC
		RpcId(1, nameof(ServerReceiveDeclareRequest), (int)currentOption, myPeerId);
		// 假设服务器 id = 1

	}
	private void OnSelectCardEvent(int[] ids)
	{
		selectCards = ids;
	}
	private void OnSetDeclareEvent(DeclareOption option)
	{
		Declare(option);
	}

	private void OnJudgeDeclareEvent(bool isValid)
	{
		// 服务器得知按下了叫主按钮然后关闭一些UI显示
		if (isValid) // 合法那么显示确定按钮
			DeclareButtonPressed();
		else // 不合法都取消
			SetInVisiable();
	}
	private void OnJudgeConfirmEvent(bool isValid)
	{
		if (isValid)
		{
			ConfirmButtonPressed();
		}
	}

	private void Declare(DeclareOption option)
	{
		currentOption = option;
		isDeclare = true;
		Visible = true;
		darkDeclareContainer.Visible = false;
		switch (option)
		{
			case DeclareOption.NONE:
				Visible = false;
				break;
			case DeclareOption.BRIGHT_TRUMP:
				declareButton.Text = "亮主";
				break;
			case DeclareOption.COUNTER_TRUMP:
				declareButton.Text = "反主";
				break;
			case DeclareOption.DARK_TRUMP:
				Visible = false;
				darkDeclareContainer.Visible = true;
				break;
		}
	}
	private void DeclareButtonPressed()
	{
		Visible = true;
		IsDeclare = false;
		darkDeclareContainer.Visible = false;
	}

	private void ConfirmButtonPressed()
	{
		// 重置按钮显示（下次可继续叫主）
		IsDeclare = true;
		Visible = false;
	}

	private void SetInVisiable()
	{
		Visible = false;
		darkDeclareContainer.Visible = false;
	}
	#region 网络信息
	// RPC 由服务器实现
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void ServerReceiveDeclareRequest(int option, long peerId)
	{
		// 这里服务器可以判断玩家是否真的有资格叫主，option是服务器想要的叫主类型
		// 如果有资格，DealManager.CreateTaskCompletionSource等
		DealEvent.OnDeclareRequestEvent((DeclareOption)option, peerId);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void ServerConfirmDeclare(long peerId, int optionInt, int[] ids)
	{
		// GD.Print($"服务器收到玩家 {peerId} 确认叫主，类型 {optionInt}, 花色 {suitInt}");
		DealEvent.OnConfirmRequestEvent((DeclareOption)optionInt, peerId, ids);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcSendCancelDarkDeclare()
	{
		DealEvent.OnCancelRequestEvent();
	}
	#endregion
}
