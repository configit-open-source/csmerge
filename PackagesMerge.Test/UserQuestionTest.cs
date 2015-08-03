using System;
using System.Xml.Linq;
using CsMerge.Core;
using CsMerge.Core.Exceptions;
using CsMerge.UserQuestion;
using NUnit.Framework;

namespace PackagesMerge.Test {

  [TestFixture]
  public class UserQuestionTest {

    [Test]
    public void UserQuestionLiteralWithDescriptionOption_NullValue() {
      var option = new UserQuestionLiteralWithDescriptionOption<string>( "A", "Option A", null, "WasNull" );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "(A) Option A:\r\nWasNull\r\n" ) );
      Assert.That( option.GetValue(), Is.Null );
    }

    [Test]
    public void UserQuestionLiteralWithDescriptionOption_NotValue() {
      var option = new UserQuestionLiteralWithDescriptionOption<string>( "A", "Option A", "Not Null Value", "WasNull" );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "(A) Option A:\r\nNot Null Value\r\n" ) );
      Assert.That( option.GetValue(), Is.EqualTo( "Not Null Value" ) );
    }

    [Test]
    public void ReferenceQuestion() {
      var optionValue = new Reference( "Test.Reference.One", false, null, "TestHintPath" );
      var option = new UserQuestionLiteralWithDescriptionOption<Reference>( "A", "Option A", optionValue, "WasNull" );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "(A) Option A:\r\nTest.Reference.One\r\nHintPath: TestHintPath\r\nSpecificVersion: False\r\n" ) );
      Assert.That( option.GetValue(), Is.EqualTo( optionValue ) );
    }

    [Test]
    public void ProjectReferenceQuestion() {
      var optionValue = new ProjectReference( "ProjPath", new Guid( "76606c7b-5bf1-497c-9b0f-9695a6a8788d" ), "ProjectName" );
      var option = new UserQuestionLiteralWithDescriptionOption<ProjectReference>( "A", "Option A", optionValue, "WasNull" );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "(A) Option A:\r\nName: ProjectName\r\nKey: 76606c7b-5bf1-497c-9b0f-9695a6a8788d\r\nPath: ProjPath\r\n" ) );
      Assert.That( option.GetValue(), Is.EqualTo( optionValue ) );
    }

    [Test]
    public void RawItemQuestion() {
      var xElement = XElement.Parse( "<Compile Include=\"File.cs\" />" );
      var optionValue = new RawItem( xElement );
      var option = new UserQuestionLiteralWithDescriptionOption<RawItem>( "A", "Option A", optionValue, "WasNull" );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "(A) Option A:\r\n<Compile Include=\"File.cs\" />\r\n" ) );
      Assert.That( option.GetValue(), Is.EqualTo( optionValue ) );
    }

    [Test]
    public void PackageQuestion() {
      var optionValue = new ConfigitPackageReference( "TestPackageId", "1.2.3.4", "net45" );
      var option = new UserQuestionLiteralWithDescriptionOption<ConfigitPackageReference>( "A", "Option A", optionValue, "WasNull" );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "(A) Option A:\r\nId: TestPackageId\r\nVersion: 1.2.3.4\r\nTargetFramework: net45\r\n\r\n" ) );
      Assert.That( option.GetValue(), Is.EqualTo( optionValue ) );
    }

    [Test]
    public void LiteralQuestion() {
      var option = new UserQuestionLiteralOption<bool>( "Y", "Yes", true );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "(Y) Yes\r\n" ) );
      Assert.That( option.GetValue(), Is.EqualTo( true ) );
    }

    [Test]
    public void ActionQuestion() {
      var option = new UserQuestionActionOption<bool>( "A", "Option A", () => true );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "(A) Option A\r\n" ) );
      Assert.That( option.GetValue(), Is.EqualTo( true ) );
    }

    [Test]
    public void ExceptionQuestion() {
      var option = new UserQuestionExceptionOption<Reference, MergeAbortException>( "A", "Abort" );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "(A) Abort\r\n" ) );
      Assert.Throws<MergeAbortException>( () => option.GetValue() );
    }

    [Test]
    public void UserQuestionLiteralOption_WhenNoKeyThenKeyNotRendered() {
      var option = new UserQuestionLiteralOption<bool>( "", "Option A (Not Available)", false );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "Option A (Not Available)\r\n" ) );
    }

    [Test]
    public void UserQuestionLiteralWithDescriptionOption_WhenNoKeyThenKeyNotRendered() {
      var option = new UserQuestionLiteralWithDescriptionOption<string>( "", "Option A (Not Available)", "Item Description", "Not Available" );
      Assert.That( option.GetQuestionText(), Is.EqualTo( "Option A (Not Available):\r\nItem Description\r\n" ) );
    }

    [Test]
    public void MultipleOptionsRenderedCorrectly() {

      var options = new UserQuestionOptionsCollection<string> {
        new UserQuestionLiteralOption<string>( "A", "Option A", "This is option A" ),
        new UserQuestionLiteralWithDescriptionOption<string>( "B", "Option B", "This is option B", "WasNull"),
        new UserQuestionActionOption<string>( "C", "Option C", () => "This is option C" ),
        new UserQuestionExceptionOption<string, MergeAbortException>( "D", "Option D" )
      };

      var expectedQuestionText = "(A) Option A\r\n\r\n"
        + "(B) Option B:\r\nThis is option B\r\n\r\n"
        + "(C) Option C\r\n\r\n"
        + "(D) Option D\r\n";

      var actualQuestionText = UserQuestion<string>.BuildQuestionText( options );

      Assert.That( actualQuestionText, Is.EqualTo( expectedQuestionText ) );
    }
  }
}
