using ClinicManagement.SubWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class MedicineSellViewModel : BaseViewModel
    {

        #region Properties

        #region Commands



        public ICommand SellMedicineCommand { get; set; }
        public ICommand AddPatientCommand { get; set; }

        #endregion

        #endregion


        public MedicineSellViewModel()
        {
            InitializCommand();
        }

        private void InitializCommand()
        {

            AddPatientCommand = new RelayCommand<object>(
                      (p) =>
                      {
                        
                      },
                       (p) => true
                   );

        }

        private void LoadData()
        {
            
        }

    }
}
