using SmartX.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSIXWechatOL.Model
{
    public class WeChatUser : INotifyPropertyChanged
    {
        public long Uin { get; set; }

        /// <summary>
        ///  用户名称，一个"@"为好友，两个"@"为群组
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// 备注名称
        /// </summary>
        public string RemarkName { get; set; }

        /// <summary>
        /// 性别，0-未设置（公众号、保密），1-男，2-女
        /// </summary>
        public int Sex { get; set; }

        /// <summary>
        /// 省
        /// </summary>
        public string Province { get; set; }

        /// <summary>
        /// 市
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// 头像图片链接地址
        /// </summary>
        public string HeadImgUrl { get; set; }

        /// <summary>
        /// 优先显示备注名，为空，则显示昵称
        /// </summary>
        public string ShowName => RemarkName.IsNullOrEmpty() ? (NickName.IsNullOrEmpty() ? UserName : NickName) : RemarkName;

        private long _updateTimestamp;
        public long UpdateTimestamp
        {
            get { return _updateTimestamp; }
            set { _updateTimestamp = value; OnPropertyChanged("UpdateTimestamp"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
