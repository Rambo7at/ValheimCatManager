using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimCatManager.ValheimCatManager.Tool
{
    public class CatToolManager
    {

        private static CatToolManager _inastance;

        public static CatToolManager Instance => _inastance ?? (_inastance = new CatToolManager());

        private CatToolManager() { }


        /// <summary>
        /// 注：获取房间主题
        /// </summary>
        /// <param name="themeName"></param>
        /// <returns></returns>
        public Room.Theme GetRoomTheme(string themeName)
        {
            if (Enum.TryParse(themeName, false, out Room.Theme theme)) return theme;

            return Room.Theme.None;
        }



    }
}
