using Godot;
using System;

public partial class UserInfoUI : Control
{
	private TextureRect textureRect;
	private Label label;
	private TextureRect crown;
	private Label level;
	public override void _Ready()
	{
		textureRect = GetNode<TextureRect>("TextureRect");
		label = GetNode<Label>("Label");
		crown = GetNode<TextureRect>("Crown");
		level = GetNode<Label>("Level");
		crown.Visible = false;
	}

	public void SetLabel(string content)
	{
		label.Text = content;
	}

	public void SetCrownVisible(bool visiable)
	{
		crown.Visible = visiable;
	}

	public void SetTextureRect(string atlasPath)
	{
		// 假设 textureRect.Texture 当前是一个 AtlasTexture 类型的资源
		if (textureRect.Texture is AtlasTexture atlasTexture)
		{
			// 加载新的图集大图
			Texture2D newAtlas = GD.Load<Texture2D>(atlasPath);
			atlasTexture.Atlas = newAtlas;
			// 如果需要，也可以同时修改 Region 区域，但这里保持原区域不变
		}
		else
		{
			// 如果当前不是 AtlasTexture，就按普通纹理设置
			textureRect.Texture = GD.Load<Texture2D>(atlasPath);
			GD.Print("当前不是AtlasTexture资源");
		}
	}
	public void SetLevel(Rank rank)
	{
		level.Text = rank switch
		{
			Rank.TWO => "2",
			Rank.FIVE => "5",
			Rank.TEN => "10",
			Rank.JACK => "J",
			Rank.KING => "K",
			Rank.ACE => "A",
			_ => throw new ArgumentOutOfRangeException(nameof(rank))
		};
	}
}
