
-- Device info 
--
CREATE TABLE DeviceInfo(
	ID bigint IDENTITY(1, 1), 
	DeviceID nvarchar(100), 
	DeviceName nvarchar(100), 
	FirstRequestTime DateTime)

CREATE UNIQUE CLUSTERED INDEX IXUC_ID ON DeviceInfo(ID)
CREATE UNIQUE INDEX IXU_DeviceID ON DeviceInfo(DeviceID)
CREATE INDEX IX_RequestTime ON DeviceInfo(FirstRequestTime)
CREATE INDEX IX_DeviceNAme ON DeviceInfo(DeviceName)

-- Request / Response data
--
CREATE TABLE CityInfo(
	ID bigint IDENTITY(1, 1),
	DeviceInfoId bigint,
	RequestTime DateTime,
	CityName nvarchar(100),
	Lat decimal(10, 10),
	Lon decimal(10, 10),
	FaceVersion nvarchar(50),
	FrameworkVersion nvarchar(50),
	CIQVersion nvarchar(50),
	RequestType nvarchar(50),
	Temperature decimal(12, 6),
	Wind decimal(12, 6),
	PrecipProbability decimal(12, 6),
	BaseCurrency nvarchar(10),
	TargetCurrency nvarchar(10),
	ExchangeRate decimal(12, 6))


CREATE CLUSTERED INDEX IXC_DeviceInfoId ON CityInfo(DeviceInfoID)
CREATE INDEX IX_RequestTime ON CityInfo(RequestTime)