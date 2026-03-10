using Godot;
using System;
using System.Collections.Generic;
using Euler.Event;

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
			item.Visible = false;
		}
		UIEvent.ChangeAvatarEvent += OnChangeAvatarEvent;
		UIEvent.ChangeNameEvent += OnChangeNameEvent;
	}
	public override void _ExitTree()
	{
		UIEvent.ChangeAvatarEvent -= OnChangeAvatarEvent;
		UIEvent.ChangeNameEvent -= OnChangeNameEvent;
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
