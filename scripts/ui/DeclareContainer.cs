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
	public event Action<DeclareOption> OnConfirmPressed; // 第二个参数可以是玩家选择的主花色/方式
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
		darkDeclareButton.Pressed += OnConfirmButton;  // 暗主点击了
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
		OnConfirmPressed?.Invoke(currentOption);
	}


	private void OnDeclareButtonPressed()
	{

		// 触发事件给 ClientRequestManager
		OnDeclarePressed?.Invoke(currentOption);

	}

	public void Declare(DeclareOption option)
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
	public void DeclareButtonPressed()
	{
		Visible = true;
		IsDeclare = false;
		darkDeclareContainer.Visible = false;
		// GD.Print($"DeclareContainer说{Multiplayer.GetUniqueId()}玩家可以{currentOption}");
	}

	public void ConfirmButtonPressed()
	{
		// 重置按钮显示（下次可继续叫主）
		IsDeclare = true;
		Visible = false;
	}

	public void SetInVisiable()
	{
		Visible = false;
		darkDeclareContainer.Visible = false;
	}
}
