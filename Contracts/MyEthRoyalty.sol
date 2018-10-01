pragma solidity ^0.4.24;

contract Mortal {
    address owner;
    
    constructor() public { owner = msg.sender; }
    
    function kill() public { if (msg.sender == owner) selfdestruct(owner); }
}

contract MyEthRoyalty is Mortal{
    
    Ownership[] public ownershipRegistrar;
    event AssetPurchased(string token, address purchaser, string assetId);
    string defaultToken = "d5874f35-7e5a-419d-bc9f-c0cd7e2c51ab";

    function getAssetUrl(string assetId) public payable returns  (string){
        Ownership memory ownership = getAsset(assetId);
        if(ownership.cost<= msg.value)
        {
            ownership.owner.transfer(msg.value);
            // call service to get individual token, returning default static for a PoC
            emit AssetPurchased(defaultToken, ownership.owner, assetId);
            return ownership.url;
        }
            return "Amount sent is insufficient.";
    }
    
    function countAssets() public view returns (uint256){
        return ownershipRegistrar.length;
    }
    
    function addAsset(address contactId, string assetId, uint256 cost, string url) public {
        ownershipRegistrar.push(Ownership({
            owner: contactId,
            assetId: assetId,
            cost: cost,
            url:url
        }));
    }
    
    function getAsset(string assetId) private view returns (Ownership) {
        for (uint p = 0; p < ownershipRegistrar.length; p++) {
            if(keccak256(abi.encodePacked(ownershipRegistrar[p].assetId)) == keccak256(abi.encodePacked(assetId)))
            {
                return ownershipRegistrar[p];
            }
        }
        return;
    }
    
    
    struct Ownership{
        address owner;
        string assetId;
        uint256 cost;
        string url;
    }
}


