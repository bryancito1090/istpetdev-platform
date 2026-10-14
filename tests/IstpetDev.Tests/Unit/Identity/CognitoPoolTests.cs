using IstpetDev.Infrastructure.Identity;

namespace IstpetDev.Tests.Unit.Identity;

[Trait("Category", "Unit")]
public sealed class CognitoPoolTests
{
    [Fact]
    public void Parses_region_pool_and_issuer_from_the_authority()
    {
        var pool = CognitoPool.FromAuthority("https://cognito-idp.us-east-1.amazonaws.com/us-east-1_AbC123/");
        Assert.NotNull(pool);
        Assert.Equal("us-east-1", pool.Region);
        Assert.Equal("us-east-1_AbC123", pool.UserPoolId);
        Assert.Equal("https://cognito-idp.us-east-1.amazonaws.com/us-east-1_AbC123", pool.Issuer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://cognito-idp.us-east-1.amazonaws.com/us-east-1_AbC123")]
    [InlineData("https://identity.istpetdev.local")]
    [InlineData("https://cognito-idp.us-east-1.amazonaws.com/eu-west-1_AbC123")]
    [InlineData("https://cognito-idp.us-east-1.amazonaws.com.evil.example/us-east-1_AbC123")]
    [InlineData("http://localhost:5000")]
    [InlineData("https://localhost:5000")]
    public void Rejects_anything_that_is_not_a_cognito_issuer(string? authority) => Assert.Null(CognitoPool.FromAuthority(authority));
}
