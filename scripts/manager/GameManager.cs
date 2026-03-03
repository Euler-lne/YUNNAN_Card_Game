using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    private TableManager tableManager;

    private UIManager uiManager;

    private Card deckCard;

    private GameCore gameCore = null;

    public override void _Ready()
    {
        uiManager = GetNode<UIManager>("../CanvasLayer/UIManager");
        TableManager tableManager = GetNode<TableManager>("../TableRoot/TableManager");
        deckCard = GetNode<Card>("../TableRoot/PlayeArea/DeckCards/Card");

        // 所有客户端都初始化 DealManager
        gameCore = Multiplayer.IsServer() ? new GameCore() : null;
        DealManager.Instance.Init(gameCore, tableManager, deckCard, uiManager);

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

        gameCore.StartGame();
        // 开始发牌
        DealManager.Instance.StartDeal();
        // 开始回合
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