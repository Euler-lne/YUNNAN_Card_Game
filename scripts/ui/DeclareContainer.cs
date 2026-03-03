using Godot;
using System;

public partial class DeclareContainer : HBoxContainer
{

	private Button declareButton;
	private Button confirmButton;

	// 给 DealManager 订阅
	public event Action OnDeclarePressed;
	public event Action<DeclareOption, Suit> OnConfirmPressed; // 第二个参数可以是玩家选择的主花色/方式

	private bool isDeclare = true;
	private DeclareOption currentOption = DeclareOption.NONE;

	private bool IsDeclare
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
		isDeclare = true;

		declareButton.Pressed += OnDeclareButtonPressed;
		confirmButton.Pressed += OnConfirmButton;
	}

	private void OnConfirmButton()
	{
		GD.Print("点击了确认");
		// FIXME: 这里暂时选择 HEART，真实可以用 UI 选择花色
		Suit selectedSuit = Suit.HEART;

		// 通知 ClientRequestManager 发送确认请求
		OnConfirmPressed?.Invoke(currentOption, selectedSuit);

		// 重置按钮显示（下次可继续叫主）
		IsDeclare = true;
		Visible = false;
	}


	private void OnDeclareButtonPressed()
	{
		// 点击叫主
		GD.Print("点击了叫主");

		// 触发事件给 ClientRequestManager
		OnDeclarePressed?.Invoke();

		// 按钮显示切换为等待确认
		IsDeclare = false;
	}

	public void Declare(DeclareOption option)
	{
		currentOption = option;

		switch (option)
		{
			case DeclareOption.NONE:
				// GD.PrintErr("失去叫主资格");
				// Visible = false;  // 当前失去了叫主资格
				break;
			case DeclareOption.BRIGHTTRUMP:
				declareButton.Text = "亮主";
				isDeclare = true;
				break;
			case DeclareOption.COUNTERTRUMP:
				declareButton.Text = "反主";
				isDeclare = true;
				break;
			case DeclareOption.DARKTRUMP:
				declareButton.Text = "暗主";
				isDeclare = true;
				break;
		}
	}
}
