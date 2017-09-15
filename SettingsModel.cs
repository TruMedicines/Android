using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVImageMatch
{
    public class SettingsModel
    {
        public bool Rotate { get; set; }

        public int RotateDegree { get; set; }
        public double UniquenessThreshold { get; set; }

        public int ChannelsCount { get; set; }

        public double HessianThresh { get; set; }
        public SettingsModel()
        {
            
        }

        public SettingsModel(SettingsModel model)
        {
            Rotate = model.Rotate;
            UniquenessThreshold = model.UniquenessThreshold;
            ChannelsCount = model.ChannelsCount;
            HessianThresh = model.HessianThresh;

        }
        //public SettingsModel DefaultValue()
        //{
        //    return new SettingsModel()
        //    {
        //        Rotate = Convert.ToBoolean(AppConfiguration.Rotate()),
        //        UniquenessThreshold = Convert.ToInt32(AppConfiguration.UniquenessThreshold()),
        //        ChannelsCount = Convert.ToInt32(AppConfiguration.ChannelsCount()),
        //        HessianThresh = Convert.ToDouble(AppConfiguration.HessianThresh())
        //    };
        //}
    }
}
