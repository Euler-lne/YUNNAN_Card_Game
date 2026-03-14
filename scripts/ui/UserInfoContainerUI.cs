using Godot;
using System;
using System.Collections.Generic;
using Euler.Event;
using Euler.Global;

public partial class UserInfoContainerUI : Control
{
	private Dictionary<int, UserInfoUI> userInfoUI = [];
	public override void _Ready()
	{
		userInfoUI[0] = GetNode<UserInfoUI>("UserInfoDown");
		userInfoUI[1] = GetNode<UserInfoUI>("UserInfoRight");
		userInfoUI[2] = GetNode<UserInfoUI>("UserInfoTop");
		userInfoUI[3] = GetNode<UserInfoUI>("UserInfoLeft");
		foreach (var item in userInfoUI.Values)
		{
			// item.Visible = false;
		}
		UIEvent.ChangeAvatarEvent += OnChangeAvatarEvent;
		UIEvent.ChangeNameEvent += OnChangeNameEvent;
		UIEvent.ChangeTrumpEvent += OnChangeTrumpEvent;
		UIEvent.ChangeLevelEvent += OnChangeLevelEvent;
		UIEvent.ChangeCurrentPlayerEvent += OnChangeCurrentPlayerEvent;
	}
	public override void _ExitTree()
	{
		UIEvent.ChangeAvatarEvent -= OnChangeAvatarEvent;
		UIEvent.ChangeNameEvent -= OnChangeNameEvent;
		UIEvent.ChangeTrumpEvent -= OnChangeTrumpEvent;
		UIEvent.ChangeLevelEvent -= OnChangeLevelEvent;
		UIEvent.ChangeCurrentPlayerEvent -= OnChangeCurrentPlayerEvent;
	}

	private void OnChangeCurrentPlayerEvent(int playerSeat)
	{
		for (int i = 0; i < GameSettings.PLAYER_COUNT; i++)
		{
			userInfoUI[i].SetPanelColor(i == playerSeat);
		}
	}

	private void OnChangeLevelEvent(int seat, Rank rank)
	{
		// TODO:赢牌调用
		userInfoUI[seat].SetLevel(rank);
		seat += 2;
		seat %= GameSettings.PLAYER_COUNT;
		userInfoUI[seat].SetLevel(rank);
	}

	private void OnChangeTrumpEvent(bool isTrump, int seat)
	{
		userInfoUI[seat].SetCrownVisible(isTrump);
	}

	private void OnChangeAvatarEvent(string path, int seat)
	{
		userInfoUI[seat].SetTextureRect(path);
		userInfoUI[seat].Visible = true;
	}

	private void OnChangeNameEvent(string name, int seat)
	{
		userInfoUI[seat].SetLabel(name);
		userInfoUI[seat].Visible = true;
	}
}
