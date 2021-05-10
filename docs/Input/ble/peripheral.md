Title: Peripheral
Order: 2
---
# Device

This section deals with gatt connections and state monitoring for a device.
You should maintain a reference to a device if you intend to connect to it.

Once you have scanned and found your peripheral, you can move on to connecting to it.  BLE operates on something called GATT (Generic Attributes).  

GATT is composed of 3 components
1. Services - this is a logical grouping of characteristics
2. Characteristics - this is the MAIN area of things that you will work with.  This is where the reading, writing, & notifications take place
3. Descriptors - these are less important in terms of use cases, but do support read/write scenarios.  They are grouped under each characteristic

## Peripheral.Connect() - void

Connect simply issues a request to connect.  It may connect now or days from now when the device is nearby.  You should use IBleCentralDelegate when using this method.  We'll talk more about that later

```csharp
peripheral.Connect();
```

If you need to wait for a connection and you know your device is nearby, you can use 

```csharp
await peripheral.ConnectAwait();
```

It is important to put your own timeout on these things using RX Timeout or supplying a cancellation token as shown below

```csharp
// this will throw an exception if it times out
await peripheral.Timeout(TimeSpan.FromSeconds(10)).ConnectAwait();

// or supply your own cancellation token
var cancelSource = new CancellationTokenSource();
await peripheral.ConnectAwait(cancelSource.Token);

cancelSource.Cancel();
```


**Connect/Disconnect to a device**

```csharp
// connect
device.Connect(new GattConnectionConfig 
{
    /// <summary>
    /// This will cause disconnected devices to try to immediately reconnect.  It will cause WillRestoreState to fire on iOS. Defaults to true
    /// </summary>
    public bool IsPersistent { get;  set; } = true;

    /// <summary>
    /// Android only - If you have characteristics where you need faster replies, you can set this to high
    /// </summary>
    public ConnectionPriority Priority { get; set; } = ConnectionPriority.Normal;
});

// this will disconnect a current connection, cancel a connection attempt, and
// remove persistent connections
device.CancelConnection();
```

**Reliable Write Transactions**

_Android and UWP only_

```csharp
using (var trans = device.BeginReliableWriteTransaction()) 
{
    await trans.Write(theCharacteristicToWriteTo, bytes);
    // you should do multiple writes here as that is the reason for this mechanism
    trans.Commit();
}
```


**Pairing with a device**
```csharp
if (device.IsPairingRequestSupported && device.PairingStatus != PairingStatus.Paired) 
{
    // there is an optional argument to pass a PIN in PairRequest as well
    device.PairRequest().Subscribe(isSuccessful => {});
}
```

**Request MTU size increase**


**MTU (Max Transmission Unit)**
If MTU requests are available (Android Only - API 21+)

This is specific to Android only where this negotiation is not automatic.
The size can be up to 512, but you should be careful with anything above 255 in practice
```csharp
// Request a greater MTU size (Androd API 21+ only), but return the final negotiated value
var newMtuValue = await device.RequestMtu(255);

// iOS will return current, Android will return 20 unless changes are observed
device.GetCurrentMtu();

// iOS will return current value and return, Android will continue to monitor changes
device.WhenMtuChanged().Subscribe(...)
```

**Monitor device states**

```csharp
// This will tell you if the device name changes during a connection
device.WhenNameChanged().Subscribe(string => {});

// this will watch the connection states to the device
device.WhenStatusChanged().Subscribe(connectionState => {});

// monitor MTU size changes (droid only)
device.WhenMtuChanged().Subscribe(size => {});

```