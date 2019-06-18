using Discord;
using Discord.WebSocket;
using System.Reflection;
using System.Threading.Tasks;

namespace Casino.Discord
{
    public static class Extensions
    {
        private static DiscordSocketRestClient _restClient;

        /// <summary>
        /// Gets the users avatar url or the url of the default avatar if they don't have an avatar.
        /// </summary>
        /// <param name="user">The user you want the avatar url of.</param>
        /// <returns>A string that is the url for their avatar.</returns>
        public static string GetAvatarOrDefaultUrl(this IUser user)
        {
            return user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
        }

        /// <summary>
        /// Gets the users Nickname, or Username if they don't have one.
        /// </summary>
        /// <param name="user">The user you want the name for.</param>
        /// <returns>A string that represents the users name.</returns>
        public static string GetDisplayName(this IGuildUser user)
        {
            return user.Nickname ?? user.Username;
        }

        /// <summary>
        /// Gets a user from cache and falls back to a REST request if they aren't present in cache.
        /// </summary>
        /// <param name="client">Your client.</param>
        /// <param name="userId">The id of the user you want to fetch.</param>
        /// <returns>The user whose id corresponds to passed id, null otherwise.</returns>
        public static async Task<IUser> GetOrFetchUserAsync(this DiscordSocketClient client, ulong userId)
        {
            return client.GetUser(userId) ?? await client.Rest.GetUserAsync(userId) as IUser;
        }

        /// <summary>
        /// Gets a user from cache and falls back to a REST request if they aren't present in cache.
        /// </summary>
        /// <param name="guild">The guild you want to get the user from.</param>
        /// <param name="userId">The id of the user you want to get.</param>
        /// <returns>The guild user whose id corresponds to the passed id, null otherwise.</returns>
        public static async Task<IGuildUser> GetOrFetchUserAsync(this SocketGuild guild, ulong userId)
        {
            if (!(guild.GetUser(userId) is IGuildUser user))
            {
                if (_restClient is null)
                {
                    var type = guild.GetType();
                    var prop = type.GetProperty("Discord", BindingFlags.Instance | BindingFlags.NonPublic);

                    var client = (DiscordSocketClient)prop.GetValue(guild);
                    _restClient = client.Rest;
                }

                user = await _restClient.GetGuildUserAsync(guild.Id, userId);
            }

            return user;
        }
    }
}
