using Godot;
using System;

public partial class DeclareContainer : HBoxContainer
{

	private Button declareButton;
	private Button confirmButton;
	private Button darkDeclareButton;
	private Button cancelButton;

	private HBoxContainer darkDeclareContainer;

	// 给 DealManager 订阅
	public event Action<DeclareOption> OnDeclarePressed;
	public event Action<DeclareOption, Suit> OnConfirmPressed; // 第二个参数可以是玩家选择的主花色/方式
	public event Action OnCancelButtonPressed;

	private bool isDeclare = true;
	private DeclareOption currentOption = DeclareOption.NONE;

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
		darkDeclareButton.Pressed += OnDeclareButtonPressed;  // 暗主点击了
		confirmButton.Pressed += OnConfirmButton;
		cancelButton.Pressed += OnCancelButton;
	}

	private void OnCancelButton()
	{
		OnCancelButtonPressed?.Invoke();
		darkDeclareContainer.Visible = false;
	}

	private void OnConfirmButton()
	{
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

		// 触发事件给 ClientRequestManager
		OnDeclarePressed?.Invoke(currentOption);
		darkDeclareContainer.Visible = false;
	}

	public void Declare(DeclareOption option)
	{
		currentOption = option;
		isDeclare = true;
		switch (option)
		{
			case DeclareOption.NONE:
				// GD.PrintErr("失去叫主资格");
				// Visible = false;  // 当前失去了叫主资格
				break;
			case DeclareOption.BRIGHTTRUMP:
				declareButton.Text = "亮主";
				break;
			case DeclareOption.COUNTERTRUMP:
				declareButton.Text = "反主";
				break;
			case DeclareOption.DARKTRUMP:
				Visible = false;
				darkDeclareContainer.Visible = true;
				break;
		}
	}
}
