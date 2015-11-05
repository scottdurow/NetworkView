// QueuedLoad.cs
//

using System;
using System.Collections.Generic;

namespace ClientUI.ViewModels
{
    public class QueuedLoad
    {
        public QueuedLoad(List<string> ids, EntitySetting entity, JoinSetting join)
        {
            Ids = ids;
            Entity = entity;
            Join = join;
        }

        public List<string> Ids;
        public EntitySetting Entity;
        public JoinSetting Join;

    }
}
