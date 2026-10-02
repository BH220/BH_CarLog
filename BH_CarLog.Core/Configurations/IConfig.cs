using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH_CarLog.Core.Configurations
{
    public interface IConfig
    {
        public bool IsEncryption { get; }

        public void Load();

        public void Save();
    }
}
