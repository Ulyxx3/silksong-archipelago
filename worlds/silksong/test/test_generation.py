from .test_base import SilksongTestBase


class TestSilksongGeneration(SilksongTestBase):
    """Test seed generation and item pool consistency."""

    game = "Silksong"

    def test_item_pool_balance(self) -> None:
        """Verify that every location has an item and counts match."""
        unfilled_locations = self.multiworld.get_unfilled_locations(self.player)
        self.assertEqual(len(self.multiworld.itempool), len(unfilled_locations))

    def test_reachability(self) -> None:
        """Verify that starting region (Mosslands) and starting locations are reachable."""
        self.assertTrue(self.can_reach_region("Mosslands"))
