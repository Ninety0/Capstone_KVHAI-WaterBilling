function setupSignalRConnection(hubUrl, hubName) {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect([0, 2000, 10000, 30000])
        .build();

    connection.start().then(function () {
        toastr.info(`Connected to ${hubName}`);
    }).catch(function (err) {
        console.error(err.toString());
    });

    connection.onreconnecting((error) => {
        //toastr.warning(`Reconnecting to ${hubName}...`);
        console.log('Reconnecting...', error);
    });

    connection.onreconnected(() => {
        //toastr.success(`Reconnected to ${hubName}`);
    });

    connection.onclose((error) => {
        //toastr.error(`Disconnected from ${hubName}. Will retry connection...`);
        console.log('Connection closed', error);
    });

    // Handle app closure
    window.addEventListener('beforeunload', function (e) {
        if (connection.state === signalR.HubConnectionState.Connected) {
            // Notify the server that we're disconnecting
            connection.invoke("ClientDisconnecting").catch(function (err) {
                console.error(err.toString());
            });

            // Stop the connection
            connection.stop();
        }
    });

    return connection;
}