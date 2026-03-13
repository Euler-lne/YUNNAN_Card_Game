using Euler.Event;
using Godot;
using System;
using System.Collections.Generic;

public partial class ThrowCardUI : VBoxContainer
{
	// Called when the node enters the scene tree for the first time.
	private Button confirmButton;
	private Button cancelButton;
	private Label label;
	List<int> selectedCard = [];
	public override void _Ready()
	{
		confirmButton = GetNode<Button>("Buttons/Confirm");
		cancelButton = GetNode<Button>("Buttons/Cancel");
		label = GetNode<Label>("ThrowCardInfo");
		confirmButton.Pressed += OnConfirmButtonPressed;
		cancelButton.Pressed += OnCancelButtonPressed;

		Visible = false;
	}

	public override void _ExitTree()
	{
		confirmButton.Pressed -= OnConfirmButtonPressed;
		cancelButton.Pressed -= OnCancelButtonPressed;
	}

	private void OnConfirmButtonPressed()
	{
		RpcId(1, nameof(RpcOnPlayCardButtonPressed), selectedCard.ToArray());
		Visible = false;
	}


	private void OnCancelButtonPressed()
	{
		Visible = false;
		TurnEvent.OnCancelThrowCardEvent();
	}

	public void PlayThrowCard(List<int> selectedCard, bool isDealer)
	{
		this.selectedCard = selectedCard;
		if (isDealer) label.Text = "甩牌失败闲家将得到5分";
		else label.Text = "甩牌失败将失去5分";
		Visible = true;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcOnPlayCardButtonPressed(int[] ids)
	{
		TurnEvent.OnPlayCardButtonPressEvent(CardData.Deserialize(ids));
	}

}
