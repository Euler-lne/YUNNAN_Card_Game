using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    private TableManager tableManager;
    private GameCore core;

    private UIManager uiManager;

    private int playerCount = 1; // 服务器自己算一个

    public override void _Ready()
    {
        uiManager = GetNode<UIManager>("../CanvasLayer/UIManager");
        tableManager = GetNode<TableManager>("../TableRoot/TableManager");
        uiManager.UpdatePlayerCount(playerCount);
        uiManager.ConnectStartButtonPressed(StartGame);
        if (Multiplayer.IsServer())
        {
            Multiplayer.PeerConnected += OnPeerConnected;
        }
    }

    private void OnPeerConnected(long id)
    {
        playerCount++;
        GD.Print("有人加入，当前人数: " + playerCount);
        uiManager.UpdatePlayerCount(playerCount);
    }

    public void StartGame()
    {
        // 1️⃣ 服务器生成游戏数据
        core = new GameCore();
        core.StartGame();

        // 2️⃣ 服务器显示自己的牌
        var serverHand = core.GetPlayerHand(0);
        tableManager.ShowPlayerHand(0, serverHand);

        // 3️⃣ 服务器把客户端的牌发过去
        SendHandToClient();
    }

    private void SendHandToClient()
    {
        var peers = Multiplayer.GetPeers();

        if (peers.Length == 0)
        {
            GD.Print("还没有客户端连接");
            return; // 还没有客户端
        }

        int clientPeerId = peers[0]; // 只有一个客户端

        var clientHand = core.GetPlayerHand(1);

        int[] ids = ConvertToIds(clientHand);

        RpcId(clientPeerId, nameof(ReceiveHand), ids);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ReceiveHand(int[] cardIds)
    {
        var hand = ConvertToCards(cardIds);
        GD.Print("客户端" + Multiplayer.GetPeers()[0] + "的牌：" + string.Join(", ", hand));
        tableManager.ShowPlayerHand(0, hand);
    }

    // ==== 工具函数 ====

    private int[] ConvertToIds(List<CardData> hand)
    {
        int[] ids = new int[hand.Count];

        for (int i = 0; i < hand.Count; i++)
            ids[i] = CardData.Serialize(hand[i]); // 你自己定义的唯一ID

        return ids;
    }

    private List<CardData> ConvertToCards(int[] ids)
    {
        List<CardData> list = new();

        foreach (var id in ids)
            list.Add(CardData.Deserialize(id)); // 你自己的查表方法

        return list;
    }
}