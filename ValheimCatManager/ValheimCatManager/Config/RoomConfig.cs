using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ValheimCatManager;
using ValheimCatManager.ValheimCatManager.Config;
using ValheimCatManager.ValheimCatManager.Tool;
using static Room;

namespace ValheimCatManager.ValheimCatManager.Config
{
    public class RoomConfig
    {
        public GameObject 预制件;

        public bool 启用 = true;

        public string 主题 = string.Empty;

        public DungeonDB.RoomData GetRoomData()
        {

            if (string.IsNullOrEmpty(主题))
            {
                Debug.LogError($"执行RoomConfig.GetRoomData方法时出错，房间：[{预制件.name}]的主题名为空");
                return null;
            }


            return new DungeonDB.RoomData()
            {
                m_enabled = 启用
            };
        }

    }
}
