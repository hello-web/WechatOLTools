using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSIXWechatOL.ViewModel.Common
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        //属性改变事件  
        public event PropertyChangedEventHandler PropertyChanged;

        //当属性改变的时候，调用该方法来发起一个消息，通知View中绑定了propertyName的元素做出调整  
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
