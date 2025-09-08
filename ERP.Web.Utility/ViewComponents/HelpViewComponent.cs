//using LifeTech.ERP.Utility.Enums;
//using LifeTech.ERP.Utility.ViewModels;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.Text;
//using System.Threading.Tasks;
//using Dapper;
//using LifeTech.ERP.Utility.Repository;
//using LifeTech.Utility.Extension;

//namespace LifeTech.ERP.Utility.ViewComponents
//{
//    public class HelpViewComponent : ViewComponent
//    {

//        private readonly CompanyMainRepository _companyMainRepository;
//        private readonly DeptMainRepository _deptMainRepository;
//        private readonly EmployeeMainRepository  _employeeMainRepository;

//        public HelpViewComponent(IConfiguration config)
//        {
//            _companyMainRepository = new CompanyMainRepository(config.GetConnectionString("UtilityERP"));
//            _deptMainRepository = new DeptMainRepository(config.GetConnectionString("UtilityERP"));
//            _employeeMainRepository = new EmployeeMainRepository(config.GetConnectionString("UtilityERP"));
//        }

//        public async Task<IViewComponentResult> InvokeAsync(HelpViewComponentViewModel model)
//        {
//            var result = new HelpViewModel() { 
//                InputName = model.InputName
//                , Help = model.HelpEnum
//                , Width = model.Width
//                , HideSerach = model.HideSearch
//                , QueryString = model.RouteValues.ToQueryString()
//                , Disabled = model.Disabled
//                , SelectModeHelp = model.SelectMode
//            };
//            if (!string.IsNullOrWhiteSpace(model.ID))
//            {
//                switch (model.HelpEnum)
//                {
//                    case HelpEnum.Company:
//                        var companyData = await _companyMainRepository.GetCompanyMainData(model.ID);
//                        result.ID = companyData.ID.ToString();
//                        result.Code = companyData.CompanyCode;
//                        result.Text = companyData.ShortName;
//                        break;
//                    case HelpEnum.Dept:
//                        var DeptData = await _deptMainRepository.GetDeptMainData(model.ID);
//                        result.ID = DeptData.ID.ToString();
//                        result.Code = DeptData.DeptCode;
//                        result.Text = DeptData.DeptName;
//                        break;
//                    case HelpEnum.Employee:
//                        var employeeData = await _employeeMainRepository.GetEmployeeMainDataAsync(model.ID);
//                        result.ID = employeeData.ID.ToString();
//                        result.Code = employeeData.EmpCode;
//                        result.Text = employeeData.EmpName;
//                        break;
//                    default:
//                        break;
//                }
//            }
//            return View("_Help", result);
//        }


//    }
//}
