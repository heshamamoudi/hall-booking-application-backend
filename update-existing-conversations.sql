-- Update existing conversations to have ConversationType
-- Default all existing conversations to 'Customer' type
UPDATE ChatConversations 
SET ConversationType = 'Customer' 
WHERE ConversationType IS NULL OR ConversationType = '';

-- For conversations where CustomerId is NULL, use the CreatedByUserId to determine type
-- If the creator is a HallManager, set ConversationType to HallManager
UPDATE c
SET c.ConversationType = 'HallManager'
FROM ChatConversations c
INNER JOIN AspNetUsers u ON c.CreatedByUserId = u.Id
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'HallManager' AND c.CustomerId IS NULL;

-- If the creator is a VendorManager, set ConversationType to VendorManager
UPDATE c
SET c.ConversationType = 'VendorManager'
FROM ChatConversations c
INNER JOIN AspNetUsers u ON c.CreatedByUserId = u.Id
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'VendorManager' AND c.CustomerId IS NULL;

-- Verify the updates
SELECT 
    Id,
    ConversationType,
    CustomerId,
    CreatedByUserId,
    HallId,
    VendorId,
    Status,
    Subject
FROM ChatConversations
ORDER BY CreatedAt DESC;
