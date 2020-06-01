CREATE TABLE DeviceInfo(
	ID bigint IDENTITY(1, 1), 
	DeviceID nvarchar, 
	DeviceName nvarchar, 
	FirstRequestTime DateTime)

CREATE UNIQUE CLUSTERED INDEX IXUC_ID ON DeviceInfo(ID)
CREATE UNIQUE INDEX IXU_DeviceID ON DeviceInfo(DeviceID)
CREATE INDEX IX_RequestTime ON DeviceInfo(FirstRequestTime)
CREATE INDEX IX_DeviceNAme ON DeviceInfo(DeviceName)

SELECT * FROM DeviceInfo