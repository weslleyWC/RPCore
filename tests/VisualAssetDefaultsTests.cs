using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RPCore.Tests
{
    public class VisualAssetDefaultsTests
    {
        [Fact] public void DllCarriesMetadataForAllOriginalYtdNamesWithoutSidecarFiles()
        {
            var catalog=VisualAssetDefaults.Catalog(); var bindings=VisualAssetDefaults.Bindings();
            Assert.Equal(8,catalog.Dictionaries.Count);Assert.Equal(679,catalog.Dictionaries.Values.Sum(l=>l.Count));Assert.Equal(27,catalog.Models.Count);
            foreach(var banner in bindings.Banners.Values) Assert.NotNull(catalog.Find("PSRP_Banners",banner));
            foreach(var icon in bindings.ItemIcons.Values) Assert.NotNull(catalog.Find("PSRP_Inventory",icon));
            var resources=typeof(VisualAssetDefaults).Assembly.GetManifestResourceNames();
            Assert.Contains("RPCore.VisualDefaults.Catalog.json",resources); Assert.Contains("RPCore.VisualDefaults.Bindings.json",resources);
            Assert.DoesNotContain(resources,r=>r.EndsWith(".ytd",StringComparison.OrdinalIgnoreCase));
        }
        [Fact] public void PartialCatalogDoesNotHideExistingImagesAndKeepsCustomDimensions()
        {
            var custom=new VisualCatalog { Dictionaries=new Dictionary<string,List<TextureInfo>> { {"psrp",new List<TextureInfo>{new TextureInfo{Name="healthicon",Width=512,Height=512},new TextureInfo{Name="CustomImage",Width=64,Height=32},new TextureInfo{Name="FoodIcon",Width=0,Height=0}}} } };
            var merged=VisualAssetDefaults.Catalog(custom);
            Assert.Equal(8,merged.Dictionaries.Count);Assert.Equal(512,merged.Find("PSRP","HealthIcon").Width);Assert.NotNull(merged.Find("PSRP","OutBar100"));Assert.NotNull(merged.Find("PSRP","CustomImage"));
            Assert.Equal(256,merged.Find("PSRP","FoodIcon").Width);
            Assert.Equal(256,VisualAssetDefaults.Catalog().Find("PSRP","HealthIcon").Width);
        }
        [Fact] public void NullCatalogListsCannotRemoveBuiltinDictionaries()
        {
            var merged=VisualAssetDefaults.Catalog(new VisualCatalog { Dictionaries=null,Models=null });
            Assert.Equal(8,merged.Dictionaries.Count);Assert.Equal(27,merged.Models.Count);
        }
        [Fact] public void LocalBindingsOverrideDefaultsWithoutRemovingOtherMappings()
        {
            var custom=new VisualBindings { ItemIcons=new Dictionary<string,string>{{"WATER","milk"},{"burger",null}},Banners=null,ItemProps=null };
            var merged=VisualAssetDefaults.Bindings(custom);
            Assert.Equal("milk",merged.ItemIcons["water"]);Assert.Null(merged.ItemIcons["burger"]);Assert.NotNull(merged.ItemIcons["sandwich"]);Assert.NotEmpty(merged.Banners);
            Assert.Equal("water",VisualAssetDefaults.Bindings().ItemIcons["water"]);
        }
        [Theory] [InlineData(0,"PSRPBANNER1")] [InlineData(4999,"PSRPBANNER1")] [InlineData(5000,"PSRPBANNER2")] [InlineData(10000,"PSRPBANNER3")] [InlineData(15000,"PSRPBANNER1")] [InlineData(-1,"PSRPBANNER1")]
        public void SettingsBannerCyclesThroughExistingThreeImagesEveryFiveSeconds(int elapsed,string expected)
        { Assert.Equal(expected,ReferenceInterface.AnimatedSettingsBanner(elapsed));Assert.NotNull(VisualAssetDefaults.Catalog().Find("PSRP_Banners",expected)); }
        [Theory] [InlineData(256,256,"dimensions_match")] [InlineData(4,4,"different_resolution")] [InlineData(0,0,"invalid_resolution")] [InlineData(float.NaN,256,"invalid_resolution")]
        public void NativeResolutionReportDoesNotMislabelDifferentSizesAsProvenMissing(float width,float height,string expected)
        { var p=new TextureProbe{DictionaryLoaded=true,ExpectedWidth=256,ExpectedHeight=256,Width=width,Height=height};Assert.Equal(expected,p.Status); }
        [Fact] public void UnloadedDictionariesAndQueryErrorsAreReportedSeparately()
        { var p=new TextureProbe();Assert.Equal("dictionary_not_loaded",p.Status);p.Error="native failed";Assert.Equal("query_error",p.Status); }
        [Fact] public void DiagnosticJsonIncludesNamesExpectedActualSizesAndRuntimeFlags()
        {
            var report=new VisualDiagnosticReport{Enabled=false,Active=false};
            report.Textures.Add(new TextureProbe{Dictionary="PSRP",Texture="FoodIcon",ExpectedWidth=256,ExpectedHeight=256,Width=256,Height=256,DictionaryLoaded=true});
            var json=JObject.Parse(JsonConvert.SerializeObject(report));
            Assert.Equal("dimensions_match",(string)json["Textures"][0]["Status"]);Assert.Equal("FoodIcon",(string)json["Textures"][0]["Texture"]);Assert.False((bool)json["Enabled"]);
            Assert.Contains("scaleform_generic.rpf",(string)json["Archive"]);
        }
    }
}
