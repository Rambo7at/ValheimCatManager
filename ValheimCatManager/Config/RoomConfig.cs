using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimCatManager.Config
{
    public class RoomConfig
    {
        public GameObject 预制件;

        public bool 启用 = true;

        public Room.Theme 主题;

        public DungeonDB.RoomData GetRoomData()
        {
            return new DungeonDB.RoomData()
            {
                m_enabled = 启用,
                m_theme = 主题,
            };
        }

    }
}
