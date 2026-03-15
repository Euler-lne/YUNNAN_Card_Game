using Godot;
using System;

public partial class MainMenu : Panel
{
	private Button createGameButton;
	private Button joinGameButton;
	private Panel inputDialog;
	private LineEdit portInput;      // 创建游戏端口输入
	private LineEdit ipInput;        // 加入游戏 IP 输入
	private LineEdit joinPortInput;  // 加入游戏端口输入
	private Button confirmButton;
	private Button cancelButton;

	public override void _Ready()
	{
		createGameButton = GetNode<Button>("VBoxContainer/CreateGame");
		joinGameButton = GetNode<Button>("VBoxContainer/JoinGame");
		createGameButton.Pressed += OnPressCreateButton;
		joinGameButton.Pressed += OnPressJoinButton;

		// 获取弹窗节点
		inputDialog = GetNode<Panel>("InputDialog");
		portInput = GetNode<LineEdit>("InputDialog/PortInput");
		ipInput = GetNode<LineEdit>("InputDialog/IpInput");
		joinPortInput = GetNode<LineEdit>("InputDialog/JoinPortInput");
		confirmButton = GetNode<Button>("InputDialog/ConfirmButton");
		cancelButton = GetNode<Button>("InputDialog/CancelButton");

		inputDialog.Visible = false;
		cancelButton.Pressed += () => inputDialog.Visible = false;
	}

	private void OnPressCreateButton()
	{
		// 显示创建弹窗：只显示端口输入
		ipInput.Visible = false;
		joinPortInput.Visible = true;
		portInput.Text = "7777"; // 默认端口
								 // 先移除所有事件再添加，避免重复
		confirmButton.Pressed -= OnConfirmCreate;
		confirmButton.Pressed -= OnConfirmJoin;
		confirmButton.Pressed += OnConfirmCreate;
		inputDialog.Visible = true;
	}

	private void OnPressJoinButton()
	{
		// 显示加入弹窗：显示 IP 和端口
		ipInput.Visible = true;
		joinPortInput.Visible = true;
		ipInput.Text = "127.0.0.1";
		joinPortInput.Text = "7777";
		confirmButton.Pressed -= OnConfirmCreate;
		confirmButton.Pressed -= OnConfirmJoin;
		confirmButton.Pressed += OnConfirmJoin;
		inputDialog.Visible = true;
	}

	private void OnConfirmCreate()
	{
		if (!int.TryParse(portInput.Text, out int port))
		{
			GD.PrintErr("端口号格式错误");
			return;
		}
		NetworkManager.Instance.HostGame(port);
		ChangeSceneManger.Instance.ChangeScene("uid://dh0r6wjeod2gf");
		inputDialog.Visible = false;
	}

	private void OnConfirmJoin()
	{
		string ip = ipInput.Text;
		if (!int.TryParse(joinPortInput.Text, out int port))
		{
			GD.PrintErr("端口号格式错误");
			return;
		}
		NetworkManager.Instance.JoinGame(ip, port);
		inputDialog.Visible = false;
		// 场景切换在 OnConnectedToServer 中完成
	}
}