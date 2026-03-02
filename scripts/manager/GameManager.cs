using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    private TableManager tableManager;

    private UIManager uiManager;

    private Card deckCard;

    public override void _Ready()
    {
        uiManager = GetNode<UIManager>("../CanvasLayer/UIManager");
        TableManager tableManager = GetNode<TableManager>("../TableRoot/TableManager");
        deckCard = GetNode<Card>("../TableRoot/PlayeArea/DeckCards/Card");

        // 所有客户端都初始化 DealManager
        DealManager.Instance.Init(Multiplayer.IsServer() ? new GameCore() : null, tableManager, deckCard);

        if (Multiplayer.IsServer())
        {
            NetworkManager.Instance.AssignServerSeat();
            uiManager.UpdatePlayerCount(NetworkManager.Instance.TotalPlayers);
            uiManager.ConnectStartButtonPressed(StartGame);
            NetworkManager.Instance.OnTotalPlayersChanged += uiManager.UpdatePlayerCount;
        }
    }

    public void StartGame()
    {
        if (!Multiplayer.IsServer())
            return;

        // 通知所有人显示牌堆
        Rpc(nameof(RpcStartGame));
        GameCore core = DealManager.Instance.GameCore; // 或直接传给 DealManager
        core.StartGame();
        // 开始发牌
        DealManager.Instance.StartDeal();
    }

    public override void _ExitTree()
    {
        if (Multiplayer.IsServer())
        {
            NetworkManager.Instance.OnTotalPlayersChanged -= uiManager.UpdatePlayerCount;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcStartGame()
    {
        deckCard.Visible = true;
    }
}