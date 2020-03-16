using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModelLib;

namespace N3Imager.ViewModel
{
    public class StatusMessage
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    class StatusViewModel : ViewModelBase
    {

        #region Properties
        
        ObservableCollection<StatusMessage> _messages = new ObservableCollection<StatusMessage>();
        public ObservableCollection<StatusMessage> Messages
        {
            get { return _messages; }
        }
        void _messages_CollectionChanged(object sender, 
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("CurrentMessage");
        }

        public string CurrentMessage
        {
            get
            {
                if (_messages.Count > 0)
                {
                    return _messages.OrderBy(p => p.Timestamp).Last().Message;
                }
                else
                    return string.Empty;
            }
        }

        byte _busy;
        public byte Busy
        {
            get { return _busy; }
            set
            {
                _busy = value;
                OnPropertyChanged("Busy");
            }
        }

        #endregion

        public StatusViewModel()
        {
            base.DisplayName = "StatusViewModel";
            _messages.CollectionChanged += 
                new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_messages_CollectionChanged);
        }

    }
}
