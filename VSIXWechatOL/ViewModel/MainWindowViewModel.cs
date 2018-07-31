using SmartXCore;
using SmartXCore.Core;
using SmartXCore.Event;
using SmartXCore.Extensions;
using SmartXCore.Model;
using SmartXCore.Module;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using VSIXWechatOL.Core;
using VSIXWechatOL.Model;
using VSIXWechatOL.ViewModel.Common;

namespace VSIXWechatOL.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private MainWindowControl _mainWindow;
        private WebWeChatClient _client;
        private bool _loginState;
        public MainWindowViewModel(MainWindowControl mainWindow)
        {
            _mainWindow = mainWindow;
            _loginState = false;

        }

        #region 字段属性

        private StringBuilder _logs;
        public string Logs
        {
            get
            {
                if (_logs == null)
                    _logs = new StringBuilder();
                return _logs.ToString();
            }
            set
            {
                _logs.AppendLine(value);
                RaisePropertyChanged("Logs");
            }
        }

        //二维码
        private BitmapImage _qrCodeImage;
        public BitmapImage QrCodeImage
        {
            get { return _qrCodeImage; }
            set
            {
                _qrCodeImage = value;
                _qrCodeImage.Freeze();
                RaisePropertyChanged("QrCodeImage");
            }
        }

        //最近联系人
        private ObservableCollection<WeChatUser> _contact_Latest = new ObservableCollection<WeChatUser>();
        public ObservableCollection<WeChatUser> Contact_Latest
        {
            get
            {
                return _contact_Latest;
            }

            set
            {
                _contact_Latest = value;
                RaisePropertyChanged("Contact_Latest");
            }
        }

        //最近联系人_被选中人
        private object _selected_Contact_latest = new object();
        public object Selected_Contact_latest
        {
            get
            {
                return _selected_Contact_latest;
            }

            set
            {
                _selected_Contact_latest = value;
                RaisePropertyChanged("Selected_Contact_latest");
            }
        }

        //@我
        public string MySelf = string.Empty;

        public string CurrentUserName = string.Empty;
        //当前聊天人
        private string _currentUserShowName = string.Empty;
        public string CurrentUserShowName
        {
            get
            {
                return _currentUserShowName;
            }

            set
            {
                _currentUserShowName = value;
                RaisePropertyChanged("CurrentUserShowName");
            }
        }


        #endregion


        #region 方法

        public void LoadInit()
        {
            ShowLoginPage(false);
            ShowChatPage(false);

            _client = WebWeChatClient.Build(new NotifyEventListener(NotifyEventListener));

            //判断登录状态
            if (!_loginState)
            {
                Start();//开始登录
            }

        }

        public void Start()
        {
            Task.Run(() =>
            {
                var isOk = false;
                do
                {
                    isOk = _client.Start();
                } while (!isOk);
            });

        }

        public void ReLogin()
        {
            ShowLoginPage(false);
            ShowChatPage(false);

            _loginState = false;

            _client.Stop();

            CurrentUserShowName = null;
            CurrentUserName = null;

            PrintLog("重新登录");
            Start();
        }

        public Task NotifyEventListener(IContext client, NotifyEvent notifyEvent)
        {
            switch (notifyEvent.Type)
            {
                case NotifyEventType.QRCodeReady:
                    {
                        var bytes = notifyEvent.Target as byte[];
                        var image = ByteArrayToBitmapImage(bytes);
                        LoadQrCodeImage(image);
                        PrintLog("请使用手机微信扫描二维码");
                        break;
                    }

                case NotifyEventType.QRCodeScanCode:
                    PrintLog("已扫描，等待登录...");
                    break;

                case NotifyEventType.QRCodeSuccess:
                    PrintLog("已确认登录");
                    PrintLog("开始获取联系人...");
                    break;

                case NotifyEventType.QRCodeInvalid:
                    PrintLog("二维码已失效");
                    ReLogin();
                    break;

                case NotifyEventType.LoginSuccess:
                    var store = client.GetModule<StoreModule>();
                    PrintLog($"获取好友列表成功，共{store.FriendCount}个");
                    PrintLog($"获取公众号|服务号列表成功，共{store.PublicUserCount}个");
                    break;

                case NotifyEventType.BeginSyncCheck:
                    var target = notifyEvent.Target as string;
                    PrintLog("开启同步检测..." + target);
                    break;

                case NotifyEventType.SyncCheckSuccess:
                    LoginSuccess();
                    PrintLog("同步检测成功");
                    PrintLog("开始循环监听消息...");
                    break;

                case NotifyEventType.SyncCheckError:
                    PrintLog("同步检测失败");
                    ReLogin();
                    break;

                case NotifyEventType.Message:
                    {
                        //处理消息
                        var msg = (Message)notifyEvent.Target;
                        MessageHandle(client, msg);
                        break;
                    }

                case NotifyEventType.Offline:
                    PrintLog("微信已离线");
                    ReLogin();
                    break;

                case NotifyEventType.Error:
                    var error = notifyEvent.Target as string;
                    PrintLog(error);
                    break;

                default:
                    PrintLog(notifyEvent.Type.GetFullDescription());
                    break;

            }
            return Task.FromResult(0);
        }

        /// <summary>
        /// 二维码转换
        /// </summary>
        private BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
        {
            BitmapImage bmp = null;

            try
            {
                bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = new MemoryStream(byteArray);
                bmp.EndInit();
            }
            catch
            {
                bmp = null;
            }

            return bmp;
        }

        public void AddContactLatest(ContactMember item)
        {
            var weChatUser = new WeChatUser()
            {
                Uin = item.Uin,
                UserName = item.UserName,
                NickName = item.NickName,
                RemarkName = item.RemarkName,
                Sex = item.Sex,
                Province = item.Province,
                City = item.City,
                HeadImgUrl = item.HeadImgUrl
            };

            //不存在 新增，存在 则排序到最前
            var isContains = false;
            foreach (var user in Contact_Latest)
            {
                if (user.UserName == weChatUser.UserName)
                {
                    isContains = true;
                    break;
                }
            }

            if (!isContains)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Contact_Latest.Insert(0, weChatUser);
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var old = Contact_Latest.FirstOrDefault(x => x.UserName == weChatUser.UserName);
                    Contact_Latest.Remove(old);
                    Contact_Latest.Insert(0, weChatUser);
                });

            }

        }

        public void ShowLoginPage(bool isShow)
        {
            _mainWindow.loginPage.Dispatcher.Invoke(() =>
            {
                _mainWindow.loginPage.Visibility = isShow ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            });
        }

        public void ShowChatPage(bool isShow)
        {
            _mainWindow.chatPage.Dispatcher.Invoke(() =>
            {
                _mainWindow.chatPage.Visibility = isShow ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            });
        }

        /// <summary>
        /// 打印日志
        /// </summary>
        public void PrintLog(string log)
        {
            Logs = log;
            _mainWindow.logListScroll.Dispatcher.Invoke(() =>
            {
                _mainWindow.logListScroll.ScrollToEnd();//滚动条到末尾
            });
        }

        /// <summary>
        /// 加载二维码
        /// </summary>
        public void LoadQrCodeImage(BitmapImage image)
        {
            QrCodeImage = image;
            ShowLoginPage(true);
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        public void MessageHandle(IContext client, Message msg)
        {
            MySelf = client.GetModule<SessionModule>().User.UserName;
            if (msg.FromUserName == MySelf)
                return;//不处理自己发的消息
            var groupName = string.Empty;
            var fromName = string.Empty;
            var content = string.Empty;
            if (msg.IsGroup() && msg.MsgType != MessageType.System)
            {
                //群消息，获取群成员信息
                var match = Regex.Match(msg.Content, "(.+?):<br\\/>(.*)");
                fromName = match.Groups[1].Value;
                content = match.Groups[2].Value;
                var contactMember = client.GetModule<StoreModule>().ContactMemberDic;
                var groupId = msg.FromUserName;
                if (contactMember.ContainsKey(groupId))
                {
                    groupName = contactMember[groupId].ShowName;
                    fromName = contactMember[groupId].MemberList.FirstOrDefault(x => x.UserName == fromName)?.ShowName;
                }
                else
                {
                    if (client.GetModule<IContactModule>().GetGroupMember(groupId))
                    {
                        groupName = contactMember[groupId].ShowName;
                        fromName = contactMember[groupId].MemberList.FirstOrDefault(x => x.UserName == fromName)?.ShowName;
                    }
                    else
                        groupName = "群消息";
                }
            }
            else
            {
                fromName = msg.FromUser?.ShowName;
                content = msg.Content;
            }
            if (msg.MsgType != MessageType.Text && msg.MsgType != MessageType.System)
                content = "发送了" + msg.MsgType.GetDescription();
            var message = string.Format("{0}:{1}", fromName, content);
            if (!groupName.IsNullOrEmpty())
                message = string.Format("[{0}]{1}", groupName, message);
            //文字消息
            if (msg.MsgType == MessageType.Text)
                PrintLog(message);
            else
                PrintLog(message);

            AddContactLatest(msg.FromUser);//最近联系人
            LocalStore.Store.Set(msg.FromUserName, message);//本地缓存
            if (!string.IsNullOrEmpty(CurrentUserShowName) && CurrentUserName == msg.FromUserName)
                AddChatMessage(message);

        }

        /// <summary>
        /// 登录成功
        /// </summary>
        public void LoginSuccess()
        {
            _loginState = true;
            //隐藏登录页
            ShowLoginPage(false);

            //加载最近联系人
            Task.Run(() =>
            {
                var store = _client.GetModule<StoreModule>();
                foreach (var item in store.LatestContactMember)
                {
                    AddContactLatest(item);
                }
            });


        }

        public void ClearChatMessage()
        {
            _mainWindow.chatList.Dispatcher.Invoke(() =>
            {
                _mainWindow.chatList.Document.Blocks.Clear();
            });
        }

        public void ScrollToEndChatMessage()
        {
            _mainWindow.chatList.Dispatcher.Invoke(() =>
            {
                _mainWindow.chatList.ScrollToEnd();
            });
        }
        public void AddChatMessage(string msg)
        {
            AddChatMessage(new List<string>() { msg });
        }

        public void AddChatMessage(List<string> msgs)
        {
            if (string.IsNullOrEmpty(CurrentUserShowName))
                return;

            if (msgs == null || msgs.Count == 0)
                return;

            _mainWindow.chatList.Dispatcher.Invoke(() =>
            {
                foreach (var msg in msgs)
                {
                    Paragraph paragraph = new Paragraph();
                    paragraph.Inlines.Add(msg);
                    _mainWindow.chatList.Document.Blocks.Add(paragraph);
                }
                _mainWindow.chatList.ScrollToEnd();
            });
        }

        public string GetSendMessage()
        {
            var msg = string.Empty;
            _mainWindow.sendMessage.Dispatcher.Invoke(() =>
            {
                msg = _mainWindow.sendMessage.Text;
            });
            return msg;
        }

        public void ClearSendMessage()
        {
            _mainWindow.sendMessage.Dispatcher.Invoke(() =>
            {
                _mainWindow.sendMessage.Text = "";
            });
        }
        #endregion


        #region 事件

        private RelayCommand _loadCommand;
        public RelayCommand LoadCommand
        {
            get
            {
                return _loadCommand ?? (_loadCommand = new RelayCommand(LoadInit));
            }
        }

        private RelayCommand _selectionChangedCommand;
        public RelayCommand SelectionChangedCommand
        {
            get
            {
                return _selectionChangedCommand ?? (_selectionChangedCommand = new RelayCommand(() =>
                {
                    if (Selected_Contact_latest is WeChatUser)
                    {
                        var user = Selected_Contact_latest as WeChatUser;
                        ShowChatPage(true);
                        CurrentUserShowName = user.ShowName;
                        CurrentUserName = user.UserName;

                        ClearChatMessage();
                        var key = user.UserName;
                        var lists = LocalStore.Store.Get(key);
                        AddChatMessage(lists);
                        ScrollToEndChatMessage();
                    }
                }));
            }
        }


        private RelayCommand _sendMessageCommand;
        public RelayCommand SendMessageCommand
        {
            get
            {
                return _sendMessageCommand ?? (_sendMessageCommand = new RelayCommand(() =>
                {
                    var text = GetSendMessage();
                    if (!string.IsNullOrEmpty(CurrentUserName))
                    {
                        var chatModule = _client.GetModule<IChatModule>();
                        var msg = MessageSent.CreateTextMsg(text, MySelf, CurrentUserName);
                        if (chatModule.SendMsg(msg))
                        {
                            ClearSendMessage();
                            text = "我:" + text;
                            AddChatMessage(text);
                        }
                    }

                }));
            }
        }
        #endregion



    }
}
