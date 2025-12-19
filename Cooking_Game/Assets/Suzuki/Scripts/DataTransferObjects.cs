using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ホストのメッセージ送信用DTO(Data Transfer Object)クラス
/// </summary>
[System.Serializable]
public class HostMessageDto
{
    [SerializeField] List<TeamDetaDto> teams;
    public List<TeamDetaDto> Teams => teams;
    [SerializeField] List<CursorInfo.Mode> canModes;
    public List<CursorInfo.Mode> CanModes => canModes;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    HostMessageDto() { }

    /// <summary>
    /// コンストラクタ（引数あり）
    /// </summary>
    /// <param name="systemManager">システムマネージャー</param>
    /// <param name="canModes">移行できるモード</param>
    public HostMessageDto(SystemManager systemManager, List<CursorInfo.Mode> canModes)
    {
        // MonoBehavoirを含むTeamのリストから、DTOのリストへ変換（ディープコピー、元のオブジェクトとの参照を切って完全にデータが独立したコピーを作ること）する
        this.teams = new List<TeamDetaDto>();
        foreach(SystemManager.Team team in systemManager.Teams)
        {
            // 新しいDTOクラスに必要なデータだけ抽出して追加
            this.teams.Add(new TeamDetaDto(team, systemManager.CurrentPhase));
        }

        this.canModes = canModes;
    }
}

/// <summary>
/// チームの情報を持つDTO(Data Transfer Object)クラス
/// </summary>
[System.Serializable]
public class TeamDetaDto
{
    [SerializeField] TeamColor color;
    public TeamColor Color => color;

    [SerializeField] SystemManager.GamePhase phase;
    public SystemManager.GamePhase Phase => phase;

    /// <summary>
    /// JsonUtility用のデフォルトコンストラクタ
    /// </summary>
    public TeamDetaDto() { }

    /// <summary>
    /// Team(元のクラス)からデータを抽出するためのコンストラクタ
    /// </summary>
    /// <param name="sourceTeam">抽出元のチームクラス</param>
    /// <param name="currentPhase">現在のフェーズ</param>
    public TeamDetaDto(SystemManager.Team sourceTeam, SystemManager.GamePhase currentPhase)
    {
        this.color = sourceTeam.Color;
        this.phase = currentPhase;
    }
}