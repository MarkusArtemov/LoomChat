using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Collections.ObjectModel;
using De.Hsfl.LoomChat.Common.Models; // Anpassen, wo dein User liegt

namespace De.Hsfl.LoomChat.Client.Converter
{
    public class UserIdToNameConverter : IMultiValueConverter
    {
        // Wir erwarten 2 Werte: [0] => SenderUserId, [1] => Users
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2
                && values[0] is int senderId
                && values[1] is ObservableCollection<User> allUsers)
            {
                var user = allUsers.FirstOrDefault(u => u.Id == senderId);
                if (user != null)
                {
                    return user.Username;
                }
            }
            return "Unknown";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
