using System;
using System.Linq;
using Newtonsoft.Json;
using Xunit;
namespace RPCore.Tests
{
    public class PdmPayrollPolicyTests
    {
        static readonly Action Save=()=>{};
        static WalletData Company()
        {
            var w=new WalletData{Bank=200000,Cash=777};Assert.True(PdmTransactions.Purchase(w,PdmDefaults.Load(),"own",Save,out _));Assert.True(PdmTransactions.Transfer(w,100000,true,"capital",Save,out _));
            var staff=PdmEnterpriseDefaults.Merge(PdmSecurityDefaults.Merge(PdmPremisesDefaults.Merge(PdmStaffDefaults.Load(),PdmPremisesDefaults.Load()),PdmSecurityDefaults.Load()),PdmEnterpriseDefaults.Load());
            foreach(var role in staff.Roles)Assert.True(PdmStaffTransactions.Hire(w,staff,role.Id,"hire:"+role.Id,Save,out _));return w;
        }
        static string Json(object o)=>JsonConvert.SerializeObject(o);
        [Fact] public void ExistingConfigWithoutNewFieldUsesHourlyPayrollAndOriginalIsUntouched()
        {
            var source=Newtonsoft.Json.Linq.JObject.Parse(Json(PdmEnterpriseDefaults.Load()));source.Remove("PayrollSeconds");var enterprise=source.ToObject<PdmEnterpriseOptions>();Assert.True(enterprise.Valid);Assert.Equal(3600,enterprise.PayrollSeconds);var staff=PdmStaffDefaults.Load();string before=Json(staff);var resolved=PdmPayrollPolicy.Resolve(staff,enterprise);Assert.Equal(3600,resolved.PeriodSeconds);Assert.Equal(before,Json(staff));
            var w=Company();w.Pdm.Staff.Employees.Clear();Assert.True(PdmStaffTransactions.Hire(w,resolved,"mechanic","hourly",Save,out _));Assert.Equal(3600,w.Pdm.Staff.Employees[0].PeriodSeconds);
        }
        [Theory] [InlineData(0)] [InlineData(1)] [InlineData(150)] [InlineData(299)]
        public void MigrationPreservesFractionOfCurrentPeriodBalancesDebtsAndEveryRole(int elapsed)
        {
            var w=Company();foreach(var e in w.Pdm.Staff.Employees){e.Seconds=elapsed;e.Owed=2*e.Wage;}w.Pdm.Staff.FormerDebt=123;
            int cash=w.Pdm.Cash,bank=w.Bank;long paid=w.Pdm.Staff.Paid;var wages=w.Pdm.Staff.Employees.Select(e=>e.Wage).ToArray();
            Assert.True(PdmPayrollPolicy.NeedsAlignment(w.Pdm,3600));Assert.True(PdmPayrollPolicy.Align(w,3600,"align",Save,out bool changed));Assert.True(changed);
            Assert.All(w.Pdm.Staff.Employees,e=>{Assert.Equal(3600,e.PeriodSeconds);Assert.Equal(elapsed*12,e.Seconds);Assert.Equal(2*e.Wage,e.Owed);});Assert.Equal(wages,w.Pdm.Staff.Employees.Select(e=>e.Wage));Assert.Equal(cash,w.Pdm.Cash);Assert.Equal(bank,w.Bank);Assert.Equal(777,w.Cash);Assert.Equal(123,w.Pdm.Staff.FormerDebt);Assert.Equal(paid,w.Pdm.Staff.Paid);
            string after=Json(w);Assert.True(PdmPayrollPolicy.Align(w,3600,"again",()=>throw new Exception(),out changed));Assert.False(changed);Assert.Equal(after,Json(w));
        }
        [Fact] public void NewAndAlreadyHiredStaffOnlyAccrueOneWageAfterFullActiveHour()
        {
            var w=Company();Assert.True(PdmPayrollPolicy.Align(w,3600,"align",Save,out _));
            for(int i=0;i<59;i++)Assert.True(PdmStaffTransactions.Work(w,60,"minute:"+i,Save,out _));
            Assert.All(w.Pdm.Staff.Employees,e=>Assert.Equal(0,e.Owed));Assert.True(PdmStaffTransactions.Work(w,59,"last-seconds",Save,out _));Assert.All(w.Pdm.Staff.Employees,e=>Assert.Equal(0,e.Owed));
            var loaded=JsonConvert.DeserializeObject<WalletData>(Json(w));Assert.All(loaded.Pdm.Staff.Employees,e=>Assert.Equal(3599,e.Seconds));
            Assert.True(PdmStaffTransactions.Work(loaded,1,"hour",Save,out _));Assert.All(loaded.Pdm.Staff.Employees,e=>{Assert.Equal(e.Wage,e.Owed);Assert.Equal(0,e.Seconds);});
            Assert.True(PdmStaffTransactions.Work(loaded,1,"hour",()=>throw new Exception(),out bool changed));Assert.False(changed);Assert.All(loaded.Pdm.Staff.Employees,e=>Assert.Equal(e.Wage,e.Owed));
        }
        [Fact] public void MigrationDiskFailureRollsBackTimePeriodAndMetadataTogether()
        {
            var w=Company();foreach(var e in w.Pdm.Staff.Employees){e.Seconds=140;e.Owed=5;}
            var obj=Newtonsoft.Json.Linq.JObject.Parse(Json(w));obj["Pdm"]["Staff"]["Future"]="keep";w=JsonConvert.DeserializeObject<WalletData>(obj.ToString());string before=Json(w);
            Assert.Throws<System.IO.IOException>(()=>PdmPayrollPolicy.Align(w,3600,"align",()=>throw new System.IO.IOException(),out _));Assert.Equal(before,Json(w));Assert.True(PdmPayrollPolicy.Align(w,3600,"align",Save,out _));Assert.Equal("keep",Newtonsoft.Json.Linq.JObject.Parse(Json(w))["Pdm"]["Staff"]["Future"]);
        }
        [Fact] public void CustomContractIntervalsAreRetainedAndCustomConfigurationIsRespected()
        {
            var original=PdmStaffDefaults.Load();original.PeriodSeconds=900;string before=Json(original);Assert.Equal(900,PdmPayrollPolicy.Resolve(original,PdmEnterpriseDefaults.Load()).PeriodSeconds);Assert.Equal(before,Json(original));
            var w=Company();w.Pdm.Staff.Employees[0].PeriodSeconds=900;w.Pdm.Staff.Employees[0].Seconds=120;Assert.True(PdmPayrollPolicy.Align(w,3600,"align",Save,out _));Assert.Equal(900,w.Pdm.Staff.Employees[0].PeriodSeconds);Assert.Equal(120,w.Pdm.Staff.Employees[0].Seconds);
        }
        [Theory] [InlineData(0)] [InlineData(59)] [InlineData(3601)]
        public void InvalidIntervalCannotChangeStoredContracts(int seconds)
        {var w=Company();string before=Json(w);Assert.False(PdmPayrollPolicy.Align(w,seconds,"align",Save,out _));Assert.Equal(before,Json(w));}
        [Fact] public void MalformedEmployeeListDoesNotCrashReadinessOrOverwriteWallet()
        {
            var w=Company();w.Pdm.Staff.Employees.Clear();w.Pdm.Staff.Employees.Add(null);string before=Json(w);Assert.False(PdmPayrollPolicy.NeedsAlignment(w.Pdm,3600));Assert.False(PdmPayrollPolicy.Align(w,3600,"align",()=>throw new Exception(),out _));Assert.Equal(before,Json(w));
        }
    }
}
