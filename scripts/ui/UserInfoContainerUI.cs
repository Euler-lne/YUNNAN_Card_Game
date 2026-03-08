using Godot;
using System;
using System.Collections.Generic;

public partial class UserInfoContainerUI : Control
{
	private Dictionary<int, UserInfoUI> userInfoUI = [];
	public override void _Ready()
	{
		userInfoUI[0] = GetNode<UserInfoUI>("UserInfoDown");
		userInfoUI[1] = GetNode<UserInfoUI>("UserInfoRight");
		userInfoUI[2] = GetNode<UserInfoUI>("UserInfoTop");
		userInfoUI[3] = GetNode<UserInfoUI>("UserInfoLeft");
	}

	public void SetUserInfo(List<string> paths, List<string> lables)
	{
		foreach (int index in userInfoUI.Keys)
		{
			UserInfoUI info = userInfoUI[index];
			info.SetLabel(lables[index]);
			info.SetTextureRect(paths[index]);
		}
	}
}
