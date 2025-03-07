using NetArchTest.Rules;

namespace Architecture.Tests;

public class ProjectReferenceTests
{
    [Fact]
    public void Domain_Should_Not_HaveDependencyOnOtherProjects()
    {
        var domainAssembly =  typeof(Sample.Domain.Placeholder).Assembly;
        
        var result = Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOn("Sample.Application")
            .And()
            .NotHaveDependencyOn("Sample.Front")
            .And()
            .NotHaveDependencyOn("Sample.Infrastructure")
            .And()
            .NotHaveDependencyOn("Sample.Api")
            .GetResult();
        
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_Can_Only_DependOnDomainProject()
    {
        var appAssembly =  typeof(Sample.Application.Placeholder).Assembly;
        
        var result = Types.InAssembly(appAssembly)
            .Should()
            .NotHaveDependencyOn("Sample.Front")
            .And()
            .NotHaveDependencyOn("Sample.Infrastructure")
            .And()
            .NotHaveDependencyOn("Sample.Api")
            .GetResult();
        
        Assert.True(result.IsSuccessful);
        
        var domainDependency = Types.InAssembly(appAssembly)
            .Should()
            .NotHaveDependencyOn("Sample.Domain")
            .GetResult();
        
        Assert.False(domainDependency.IsSuccessful);
    }

    [Fact]
    public void Infrastructure_Can_Only_DependOnDomainProject()
    {
        var appAssembly =  typeof(Sample.Infrastructure.Placeholder).Assembly;
        
        var result = Types.InAssembly(appAssembly)
            .Should()
            .NotHaveDependencyOn("Sample.Front")
            .And()
            .NotHaveDependencyOn("Sample.Application")
            .And()
            .NotHaveDependencyOn("Sample.Api")
            .GetResult();
        
        Assert.True(result.IsSuccessful);
        
        var domainDependency = Types.InAssembly(appAssembly)
            .Should()
            .NotHaveDependencyOn("Sample.Domain")
            .GetResult();
        
        Assert.False(domainDependency.IsSuccessful);
    }
}