using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ホストのメッセージ送信用DTOクラス
/// </summary>
[System.Serializable]
public class HostMessageDto
{
    [SerializeField]SystemManager systemManager;
    public SystemManager HostSystemManager => systemManager;
    [SerializeField] List<CursorInfo.Mode> canModes;
    public List<CursorInfo.Mode> CanModes => canModes;

    public HostMessageDto(SystemManager systemManager, List<CursorInfo.Mode> canModes)
    {
        this.systemManager = systemManager;
        this.canModes = canModes;
    }
}

[System.Serializable]
public class BulletSpawnMessage
{
    [SerializeField] int playerId;

}