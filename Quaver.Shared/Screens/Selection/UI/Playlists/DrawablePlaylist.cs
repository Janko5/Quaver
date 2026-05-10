using Quaver.Shared.Database.Playlists;
using Quaver.Shared.Graphics.Containers;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Screens.Selection.UI.Mapsets;
using Wobble.Bindables;
using Wobble.Graphics;
using Quaver.Shared.Skinning;

namespace Quaver.Shared.Screens.Selection.UI.Playlists
{
    public sealed class DrawablePlaylist : PoolableSprite<Playlist>
    {
        /// <inheritdoc />
        /// <summary>
        ///     Returns the height of a playlist slot including spacing/padding.
        ///     V2: 100px height + 10px spacing = 110px.
        ///     V1: 97px height (legacy).
        /// </summary>
        public override int HEIGHT { get; } = SkinManager.Skin?.UserInterfaceVersion >= 2f ? 110 : DrawableMapset.MapsetHeight;

        /// <summary>
        /// </summary>
        private DrawablePlaylistContainer DrawableContainer { get; }

        /// <summary>
        ///     If the playlist is selected
        /// </summary>
        public bool IsSelected => PlaylistManager.Selected.Value == Item;

        /// <summary>
        ///     Header panel context in mapsets list
        /// </summary>
        public MapsetScrollContainer MapsetContainer { get; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="container"></param>
        /// <param name="item"></param>
        /// <param name="index"></param>
        public DrawablePlaylist(PoolableScrollContainer<Playlist> container, Playlist item, int index) : base(container, item, index)
        {
            Size = new ScalableVector2(DrawableMapset.WIDTH, HEIGHT);

            DrawableContainer = new DrawablePlaylistContainer(this)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                UsePreviousSpriteBatchOptions = true
            };

            Alpha = 0;
            UsePreviousSpriteBatchOptions = true;

            UpdateContent(item, index);

            PlaylistManager.Selected.ValueChanged += OnPlaylistChanged;
            PlaylistManager.PlaylistMapsManaged += OnPLaylistMapsManaged;
            ModManager.ModsChanged += OnModsChanged;
        }

        public DrawablePlaylist(MapsetScrollContainer container, Playlist item) : base(null, item, 0)
        {
            MapsetContainer = container;
            Size = new ScalableVector2(DrawableMapset.WIDTH, HEIGHT);

            DrawableContainer = new DrawablePlaylistContainer(this)
            {
                Parent = this,
                Alignment = Alignment.TopRight,
                UsePreviousSpriteBatchOptions = true
            };

            Alpha = 0;
            UsePreviousSpriteBatchOptions = true;

            UpdateContent(item, 0);

            PlaylistManager.Selected.ValueChanged += OnPlaylistChanged;
            PlaylistManager.PlaylistMapsManaged += OnPLaylistMapsManaged;
            ModManager.ModsChanged += OnModsChanged;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        public override void UpdateContent(Playlist item, int index)
        {
            Item = item;
            Index = index;

            ScheduleUpdate(() =>
            {
                DrawableContainer.UpdateContent(Item, Index);

                Deselect();
            });
        }

        /// <summary>
        /// </summary>
        // ReSharper disable once InheritdocConsiderUsage
        public override void Destroy()
        {
            // ReSharper disable once DelegateSubtraction
            PlaylistManager.Selected.ValueChanged -= OnPlaylistChanged;
            ModManager.ModsChanged -= OnModsChanged;
            PlaylistManager.PlaylistMapsManaged -= OnPLaylistMapsManaged;

            base.Destroy();
        }

        /// <summary>
        /// </summary>
        public void Select() => DrawableContainer.Select();

        /// <summary>
        /// </summary>
        public void Deselect() => DrawableContainer.Deselect();

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaylistChanged(object sender, BindableValueChangedEventArgs<Playlist> e) => Deselect();

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPLaylistMapsManaged(object sender, PlaylistMapsManagedEventArgs e)
        {
            if (e.Playlist != Item)
                return;

            UpdateContent(Item, Index);
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnModsChanged(object sender, ModsChangedEventArgs e) => UpdateContent(Item, Index);

        /// <inheritdoc />
        public override void DrawToSpriteBatch()
        {
            // Do not draw the base sprite to avoid Wobble's white fallback pixel
        }
    }
}