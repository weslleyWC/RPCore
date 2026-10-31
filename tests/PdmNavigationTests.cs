using System;
using System.Linq;
using Xunit;
namespace RPCore.Tests
{
    public class PdmNavigationTests
    {
        static PdmLocation Place(string id="office",float x=-63.2041f)=>PdmLocation.At(id,"Escritorio","Escritorio","Use E no marcador",new JobPoint{Name="Custom office",X=x,Y=-1092.664f,Z=26.53896f});
        [Fact] public void NavigationUsesConfiguredCoordinatesIncludingCustomOffice()
        {var p=Place(x:123.5f);var list=PdmLocation.Available(new[]{p});Assert.Single(list);Assert.Equal(123.5f,list[0].Point.X);Assert.Equal(26.53896f,list[0].Point.Z);}
        [Theory] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(float.NegativeInfinity)]
        public void InvalidNativeDestinationsAreNotOffered(float x)=>Assert.Empty(PdmLocation.Available(new[]{Place(x:x)}));
        [Fact] public void SameCoordinatesCanDescribePurchasingVersusOfficeButIdsCannotDuplicate()
        {var office=Place();var supplier=Place("supplier");var duplicate=Place(x:999);var list=PdmLocation.Available(new[]{office,supplier,duplicate});Assert.Equal(2,list.Count);Assert.Equal(-63.2041f,list.Single(p=>p.Id=="office").Point.X);}
        [Fact] public void MissingAndMalformedEntriesAreSkippedWithoutCreatingEmptyActions()
        {var broken=Place();broken.Point=null;var missingName=Place();missingName.Name=" ";Assert.Empty(PdmLocation.Available(new[]{null,broken,missingName}));Assert.Empty(PdmLocation.Available(null));}
        [Fact] public void LocationGroupsFollowTheBusinessWorkflowNotCoordinateOrNameOrder()
        {var a=Place("one");a.Group="Showroom";var b=Place("two");b.Group="Escritorio e compras";var c=Place("three");c.Group="Logistica";Assert.Equal(new[]{"Escritorio e compras","Logistica","Showroom"},PdmLocation.Groups(new[]{a,b,c}));}
        [Fact] public void CustomGroupsRemainAvailableAfterKnownGroups()
        {var a=Place("one");a.Group="Custom";var b=Place("two");b.Group="Contratos";Assert.Equal(new[]{"Contratos","Custom"},PdmLocation.Groups(new[]{a,b,a}));}
        [Fact] public void ACustomLargeConfigurationIsBoundedAndDeterministicallySorted()
        {var all=Enumerable.Range(0,300).Select(i=>{var p=Place("point"+i);p.Name=i.ToString("D3");return p;}).Reverse();var list=PdmLocation.Available(all);Assert.Equal(256,list.Count);Assert.Equal("000",list[0].Name);Assert.Equal("255",list[255].Name);}
    }
}
