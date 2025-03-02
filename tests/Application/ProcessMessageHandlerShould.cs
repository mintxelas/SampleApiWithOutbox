﻿using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Sample.Application.ProcessMessage;
using Sample.Domain;
using Xunit;

namespace Sample.Application.Tests;

public class ProcessMessageHandlerShould
{
    private const string SomeTextToMatch = "text to match";
    private const string SomeText = "some text";
    private const string SomeExceptionMessage = "some exception message";
    private static readonly Guid SomeId = Guid.NewGuid();
    private readonly IMessageRepository repository;
    private readonly ProcessMessageHandler handler;

    public ProcessMessageHandlerShould()
    {
        repository = Substitute.For<IMessageRepository>();
        handler = new ProcessMessageHandler(repository);
    }

    [Fact]
    public async Task return_a_success_response_when_processing_succeeds()
    {
        repository.GetById(SomeId)
            .Returns(new Message(SomeId, SomeText));
        var response = await handler.Handle(new ProcessMessageRequest(SomeId, SomeTextToMatch),
            CancellationToken.None);
        Assert.IsNotType<MessageToProcessNotFoundResponse>(response);
        Assert.IsNotType<ErrorProcessingMessageResponse>(response);
        Assert.Equal("Message processed.", response.Description);
    }

    [Fact]
    public async Task return_not_found_response_when_given_message_does_not_exist_in_repository()
    {
        var response = await handler.Handle(new ProcessMessageRequest(SomeId, SomeTextToMatch),
            CancellationToken.None);
        Assert.IsType<MessageToProcessNotFoundResponse>(response);
    }

    [Fact]
    public async Task return_an_error_response_when_an_exception_occurs_during_processing()
    {
        repository.GetById(SomeId)
            .ThrowsForAnyArgs(new Exception(SomeExceptionMessage));
        var response = await handler.Handle(new ProcessMessageRequest(SomeId, SomeTextToMatch),
            CancellationToken.None);
        Assert.IsType<ErrorProcessingMessageResponse>(response);
        Assert.Equal(SomeExceptionMessage, response.Description);
    }

}