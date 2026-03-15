using Godot;
using System;

public partial class MainMenu : Panel
{
	private Button createGameButton;
	private Button joinGameButton;
	private Panel inputDialog;
	private VBoxContainer joinInputs;          // 加入游戏的输入容器（IP和端口）
	private HBoxContainer createPortContainer; // 创建游戏的端口容器
	private LineEdit createPortInput;          // 创建游戏端口输入
	private LineEdit ipInput;                   // 加入游戏 IP 输入
	private LineEdit joinPortInput;             // 加入游戏端口输入
	private Button confirmButton;
	private Button cancelButton;

	private bool isCreateMode = true; // 当前模式：true=创建，false=加入

	public override void _Ready()
	{
		// 获取节点
		createGameButton = GetNode<Button>("VBoxContainer/CreateGame");
		joinGameButton = GetNode<Button>("VBoxContainer/JoinGame");
		inputDialog = GetNode<Panel>("InputDialog");
		joinInputs = GetNode<VBoxContainer>("InputDialog/JoinInputs");
		createPortContainer = GetNode<HBoxContainer>("InputDialog/Port");
		createPortInput = GetNode<LineEdit>("InputDialog/Port/CreatePortInput");
		ipInput = GetNode<LineEdit>("InputDialog/JoinInputs/IP/IpInput");
		joinPortInput = GetNode<LineEdit>("InputDialog/JoinInputs/Port/JoinPortInput");
		confirmButton = GetNode<Button>("InputDialog/Buttons/ConfirmButton");
		cancelButton = GetNode<Button>("InputDialog/Buttons/CancelButton");

		// 绑定事件
		createGameButton.Pressed += OnPressCreateButton;
		joinGameButton.Pressed += OnPressJoinButton;
		confirmButton.Pressed += OnConfirm;
		cancelButton.Pressed += OnCancel;

		// 初始隐藏弹窗
		inputDialog.Visible = false;
	}

	public override void _ExitTree()
	{
		// 解绑所有事件
		createGameButton.Pressed -= OnPressCreateButton;
		joinGameButton.Pressed -= OnPressJoinButton;
		confirmButton.Pressed -= OnConfirm;
		cancelButton.Pressed -= OnCancel;
	}

	private void OnPressCreateButton()
	{
		isCreateMode = true;
		createPortContainer.Visible = true;
		joinInputs.Visible = false;
		createPortInput.Text = "7777";
		inputDialog.Visible = true;
	}

	private void OnPressJoinButton()
	{
		isCreateMode = false;
		createPortContainer.Visible = false;
		joinInputs.Visible = true;
		ipInput.Text = "127.0.0.1";
		joinPortInput.Text = "7777";
		inputDialog.Visible = true;
	}

	private void OnConfirm()
	{
		if (isCreateMode)
			OnConfirmCreate();
		else
			OnConfirmJoin();
	}

	private void OnCancel()
	{
		inputDialog.Visible = false;
	}

	private void OnConfirmCreate()
	{
		if (!ValidatePort(createPortInput.Text, out int port))
		{
			GD.PrintErr("端口号无效，应为1024-65535之间的整数");
			return;
		}
		NetworkManager.Instance.HostGame(port);
		ChangeSceneManger.Instance.ChangeScene("uid://dh0r6wjeod2gf");
		inputDialog.Visible = false;
	}

	private void OnConfirmJoin()
	{
		string ip = ipInput.Text;
		if (!ValidateIp(ip))
		{
			GD.PrintErr("IP地址格式不正确");
			return;
		}
		if (!ValidatePort(joinPortInput.Text, out int port))
		{
			GD.PrintErr("端口号无效");
			return;
		}
		NetworkManager.Instance.JoinGame(ip, port);
		inputDialog.Visible = false;
	}

	private bool ValidateIp(string ip)
	{
		string[] parts = ip.Split('.');
		if (parts.Length != 4) return false;
		foreach (string part in parts)
		{
			if (!int.TryParse(part, out int num)) return false;
			if (num < 0 || num > 255) return false;
		}
		return true;
	}

	private bool ValidatePort(string portText, out int port)
	{
		if (!int.TryParse(portText, out port)) return false;
		return port >= 1024 && port <= 65535;
	}
}