using ClinicManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
   public class PatientViewModel : BaseViewModel
    {
      
        private ObservableCollection<Patient> _PatientList;
        public ObservableCollection<Patient> PatientList
        {
            get => _PatientList;
            set
            {
                _PatientList = value; OnPropertyChanged();
            }
        }
        private ObservableCollection<PatientType> _PatientTypeList;
        public ObservableCollection<PatientType> PatientTypeList
        {
            get => _PatientTypeList;
            set
            {
                _PatientTypeList = value; OnPropertyChanged();
            }
        }

        public PatientViewModel() 
        {
         
            
                PatientList = new ObservableCollection<Patient>(DataProvider.Instance.Context.Patients);
                PatientTypeList = new ObservableCollection<PatientType>(DataProvider.Instance.Context.PatientTypes);
              
           
            
        }
    }
}
